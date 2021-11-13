/**
 *   Copyright (C) 2021 okaygo
 *
 *   https://github.com/misterokaygo/MapAssist/
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <https://www.gnu.org/licenses/>.
 **/

using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

using MapAssist.Structs;
using MapAssist.Types;
using MapAssist.Settings;

namespace MapAssist.Helpers
{
    class GameMemory
    {
        private static readonly string ProcessName = Encoding.UTF8.GetString(new byte[] { 68, 50, 82 });
        private static IntPtr PlayerUnitPtr;
        private static UnitAny PlayerUnit = default;
        private static int _lastProcessId = 0;
        public static List<Monster> Monsters = new List<Monster>();
        public static List<string> WarningMessages = new List<string>();
        private static Difficulty currentDifficulty = Difficulty.None;
        private static uint currentMapSeed = 0;

        unsafe public static GameData GetGameData(MapAssistConfiguration configuration)
        {
            IntPtr processHandle = IntPtr.Zero;

            try
            {
                Process[] process = Process.GetProcessesByName(ProcessName);

                Process gameProcess = null;

                IntPtr windowInFocus = WindowsExternal.GetForegroundWindow();
                if (windowInFocus == IntPtr.Zero)
                {
                    gameProcess = process.FirstOrDefault();
                }
                else
                {
                    gameProcess = process.FirstOrDefault(p => p.MainWindowHandle == windowInFocus);
                }

                if (gameProcess == null)
                {
                    throw new Exception("Game process not found.");
                }

                // If changing processes we need to re-find the player
                if (gameProcess.Id != _lastProcessId)
                {
                    ResetPlayerUnit();
                }

                _lastProcessId = gameProcess.Id;

                processHandle =
                    WindowsExternal.OpenProcess((uint)WindowsExternal.ProcessAccessFlags.VirtualMemoryRead, false, gameProcess.Id);
                IntPtr processAddress = gameProcess.MainModule.BaseAddress;
                if (PlayerUnitPtr == IntPtr.Zero)
                {
                    var expansionCharacter = Read<byte>(processHandle, IntPtr.Add(processAddress, configuration.Offsets.ExpansionCheck)) == 1;
                    var userBaseOffset = 0x30;
                    var checkUser1 = 1;
                    if (expansionCharacter)
                    {
                        userBaseOffset = 0x70;
                        checkUser1 = 0;
                    }
                    var unitHashTable = Read<UnitHashTable>(processHandle, IntPtr.Add(processAddress, configuration.Offsets.UnitHashTable));
                    foreach (var pUnitAny in unitHashTable.UnitTable)
                    {
                        var pListNext = pUnitAny;

                        while (pListNext != IntPtr.Zero)
                        {
                            var unitAny = Read<UnitAny>(processHandle, pListNext);
                            if (unitAny.Inventory != IntPtr.Zero)
                            {
                                var UserBaseCheck = Read<int>(processHandle, IntPtr.Add(unitAny.Inventory, userBaseOffset));
                                if (UserBaseCheck != checkUser1)
                                {
                                    PlayerUnitPtr = pUnitAny;
                                    PlayerUnit = unitAny;
                                    break;
                                }
                            }
                            pListNext = (IntPtr)unitAny.pListNext;
                        }

                        if (PlayerUnitPtr != IntPtr.Zero)
                        {
                            break;
                        }
                    }
                }

                if (PlayerUnitPtr == IntPtr.Zero)
                {
                    currentDifficulty = Difficulty.None;
                    currentMapSeed = 0;
                    throw new Exception("Player pointer is zero.");
                }

                /*IntPtr aPlayerUnit = Read<IntPtr>(processHandle, PlayerUnitPtr); 
    
                if (aPlayerUnit == IntPtr.Zero)
                {
                    throw new Exception("Player address is zero.");
                }*/

                var playerName = Encoding.ASCII.GetString(Read<byte>(processHandle, PlayerUnit.UnitData, 16)).TrimEnd((char)0);
                var act = Read<Act>(processHandle, (IntPtr)PlayerUnit.pAct);
                var mapSeed = (uint)0;
                if (currentMapSeed == 0)
                {
                    currentMapSeed = act.MapSeed;
                }
                mapSeed = currentMapSeed;

                if (mapSeed <= 0 || mapSeed > 0xFFFFFFFF)
                {
                    throw new Exception("Map seed is out of bounds.");
                }

                var actId = act.ActId;
                var actMisc = Read<ActMisc>(processHandle, (IntPtr)act.ActMisc);
                var gameDifficulty = Difficulty.None;
                if(currentDifficulty == Difficulty.None)
                {
                    currentDifficulty = actMisc.GameDifficulty;
                }
                gameDifficulty = currentDifficulty;

                if (!gameDifficulty.IsValid())
                {
                    throw new Exception("Game difficulty out of bounds.");
                }

                var path = Read<Path>(processHandle, (IntPtr)PlayerUnit.pPath);
                var positionX = path.DynamicX;
                var positionY = path.DynamicY;
                var room = Read<Room>(processHandle, (IntPtr)path.pRoom);
                var roomEx = Read<RoomEx>(processHandle, (IntPtr)room.pRoomEx);
                var level = Read<Level>(processHandle, (IntPtr)roomEx.pLevel);
                var levelId = level.LevelId;

                if (!levelId.IsValid())
                {
                    throw new Exception("Level id out of bounds.");
                }

                var mapShownByte = Read<UiSettings>(processHandle, IntPtr.Add(processAddress, configuration.Offsets.UiSettings)).MapShown;
                var mapShown = mapShownByte == 1;

                WarningMessages.Clear();
                Monsters = GetMobs(processHandle, IntPtr.Add(processAddress, configuration.Offsets.UnitHashTable + (128 * 8)), configuration);

                return new GameData
                {
                    PlayerPosition = new Point(positionX, positionY),
                    MapSeed = mapSeed,
                    Area = levelId,
                    Difficulty = gameDifficulty,
                    MapShown = mapShown,
                    MainWindowHandle = gameProcess.MainWindowHandle,
                    PlayerName = playerName
                };
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                ResetPlayerUnit();
                return null;
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                {
                    WindowsExternal.CloseHandle(processHandle);
                }
            }
        }
        unsafe public static List<Monster> GetMobs(IntPtr processHandle, IntPtr startAddress, MapAssistConfiguration configuration)
        {
            var monList = new List<Monster>();

            var unitHashTable = Read<UnitHashTable>(processHandle, startAddress);
            foreach (var pUnitAny in unitHashTable.UnitTable)
            {
                var pListNext = pUnitAny;

                while (pListNext != IntPtr.Zero)
                {
                    var unitAny = Read<UnitAny>(processHandle, pListNext);
                    //why would we ever comment any of our code with anything useful? :D
                    if (unitAny.Mode != 0 && unitAny.Mode != 12 && !NPCs.Dummies.TryGetValue(unitAny.TxtFileNo, out var _))
                    {

                        var monData = Read<MonsterData>(processHandle, unitAny.UnitData);
                        var monStats = Read<MonStats>(processHandle, monData.pMonStats);
                        var monNameBytes = new byte[monStats.Name.Length - 2];
                        Array.Copy(monStats.Name, 2, monNameBytes, 0, monNameBytes.Length);
                        var monName = Encoding.ASCII.GetString(monNameBytes).TrimEnd((char)0);
                        var monPath = Read<Path>(processHandle, (IntPtr)unitAny.pPath);
                        var flag = Read<uint>(processHandle, IntPtr.Add(unitAny.UnitData, 24));
                        var newMon = new Monster()
                        {
                            Position = new Point(monPath.DynamicX, monPath.DynamicY),
                            UniqueFlag = flag,
                            Immunities = GetImmunes(processHandle, unitAny)
                        };
                        var NPC = Enum.GetName(typeof(Npc), unitAny.TxtFileNo);

                        var SuperUnique = false;
                        var NameInWarnList = Array.Exists(configuration.Map.WarnImmuneNPC, element => (element == Enum.GetName(typeof(Npc), unitAny.TxtFileNo)));
                        var NameInWarnList2 = false;
                        if ((monData.MonsterType & MonsterTypeFlags.SuperUnique) == MonsterTypeFlags.SuperUnique)
                        {
                            NPCs.SuperUnique.TryGetValue(monName, out var SuperUniqueName);
                            SuperUnique = NPCs.SuperUniqueName(monName) == SuperUniqueName;
                            NameInWarnList2 = Array.Exists(configuration.Map.WarnImmuneNPC, element => (element == SuperUniqueName));
                            if (SuperUnique)
                            {
                                NPC = SuperUniqueName;
                            }
                        }
                        if (NameInWarnList || (SuperUnique && NameInWarnList2))
                        {
                            var immunestr = "";
                            foreach(Resist immunity in newMon.Immunities)
                            {
                                immunestr += immunity + ", ";
                            }
                            if (immunestr.Length > 2)
                            {
                                immunestr = immunestr.Remove(immunestr.Length - 2);
                                WarningMessages.Add(NPC + " is immune to " + immunestr);
                            }
                        }
                        monList.Add(newMon);
                    }
                    pListNext = (IntPtr)unitAny.pListNext;
                }
            }
            return monList;
        }

        public static Dictionary<Stat, int> GetStats(IntPtr processHandle, UnitAny unit)
        {
                var _statListStruct = Read<StatListStruct>(processHandle, unit.StatsListEx);
                return Read<StatValue>(processHandle, _statListStruct.Stats.FirstStatPtr, Convert.ToInt32(_statListStruct.Stats.Size)).ToDictionary(s => s.Stat, s => s.Value);
        }
        public static List<int> GetResists(IntPtr processHandle, UnitAny unit)
        {
            var stats = GetStats(processHandle, unit);

            stats.TryGetValue(Stat.STAT_DAMAGERESIST, out var dmgres);
            stats.TryGetValue(Stat.STAT_MAGICRESIST, out var magicres);
            stats.TryGetValue(Stat.STAT_FIRERESIST, out var fireres);
            stats.TryGetValue(Stat.STAT_LIGHTRESIST, out var lightres);
            stats.TryGetValue(Stat.STAT_COLDRESIST, out var coldres);
            stats.TryGetValue(Stat.STAT_POISONRESIST, out var poires);

            var _resists = new List<int> { dmgres, magicres, fireres, lightres, coldres, poires };

            return _resists;
        }
        public static List<Resist> GetImmunes(IntPtr processHandle, UnitAny unit)
        {
            var resists = GetResists(processHandle, unit);
            var immunities = new List<Resist>();
            for (var i = 0; i < 6; i++)
            {
                if (resists[i] >= 100)
                {
                    immunities.Add((Resist)i);
                }
            }
            var _immunes = immunities;
            return _immunes;
        }
        private static void ResetPlayerUnit()
        {
            PlayerUnit = default;
            PlayerUnitPtr = IntPtr.Zero;
        }

        public static T[] Read<T>(IntPtr processHandle, IntPtr address, int count) where T : struct
        {
            var sz = Marshal.SizeOf<T>();
            var buf = new byte[sz * count];
            WindowsExternal.ReadProcessMemory(processHandle, address, buf, buf.Length, out _);

            var handle = GCHandle.Alloc(buf, GCHandleType.Pinned);
            try
            {
                var result = new T[count];
                for (var i = 0; i < count; i++)
                {
                    result[i] = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject() + (i * sz), typeof(T));
                }

                return result;
            }
            finally
            {
                handle.Free();
            }
        }

        public static T Read<T>(IntPtr processHandle, IntPtr address) where T : struct
        {
            return Read<T>(processHandle, address, 1)[0];
        }
    }
}
