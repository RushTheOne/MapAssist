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
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using MapAssist.Types;

// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace MapAssist.Settings
{
    public abstract class ConfigurationBase
    {
        protected abstract void ReadFromConfiguration();
    }
    public class RenderingConfiguration : ConfigurationBase
    {
        public RenderingConfiguration()
        {
            ReadFromConfiguration();
        }

        public PointOfInterestRendering NextArea { get; set; }
        public PointOfInterestRendering PreviousArea { get; set; }
        public PointOfInterestRendering Waypoint { get; set; } 
        public PointOfInterestRendering Quest { get; set; } 
        public PointOfInterestRendering Player { get; set; }
        public PointOfInterestRendering SuperChest { get; set; } 

        protected override void ReadFromConfiguration()
        {
            NextArea = Utils.GetRenderingSettingsForPrefix("NextArea");
            PreviousArea = Utils.GetRenderingSettingsForPrefix("PreviousArea");
            Waypoint = Utils.GetRenderingSettingsForPrefix("Waypoint");
            Quest = Utils.GetRenderingSettingsForPrefix("Quest");
            Player = Utils.GetRenderingSettingsForPrefix("Player");
            SuperChest = Utils.GetRenderingSettingsForPrefix("SuperChest");
        }
    }

    public class MapConfiguration : ConfigurationBase
    {
        public MapConfiguration()
        {
            ReadFromConfiguration();
        }

        public double Opacity { get; set; }
        public bool OverlayMode { get; set; }
        public bool AlwaysOnTop { get; set; }
        public bool ToggleViaInGameMap { get; set; }
        public int Size { get; set; }
        public MapPosition Position { get; set; }
        public int UpdateTime { get; set; }
        public bool Rotate { get; set; }
        public char ToggleKey { get; set; }
        public char ZoomInKey { get; set; }
        public char ZoomOutKey { get; set; }
        public float ZoomLevel { get; set; }
        public Area[] PrefetchAreas { get; set; }
        public  Area[] HiddenAreas { get; set; } 
        public string[] WarnImmuneNPC { get; set; }
        public int WarnImmuneNPCFontSize { get; set; }
        public string WarnImmuneNPCFont { get; set; }
        public StringAlignment WarnNPCVerticalAlign { get; set; }
        public StringAlignment WarnNPCHorizontalAlign { get; set; }
        public Color WarnNPCFontColor { get; set; }
        public bool ClearPrefetchedOnAreaChange { get; set; }

        protected override void ReadFromConfiguration()
        {
            UpdateTime = ConfigurationReader.ReadInt16("UpdateTime");
            Rotate = ConfigurationReader.ReadBoolean("Rotate");
            ToggleKey = ConfigurationReader.ReadChar("ToggleKey");
            ZoomInKey = ConfigurationReader.ReadChar("ZoomInKey");
            ZoomOutKey = ConfigurationReader.ReadChar("ZoomOutKey");
            ZoomLevel = ConfigurationReader.ReadSingle("ZoomLevelDefault");
            Opacity = ConfigurationReader.ReadDouble("Opacity");
            OverlayMode = ConfigurationReader.ReadBoolean("OverlayMode");
            AlwaysOnTop = ConfigurationReader.ReadBoolean("AlwaysOnTop");
            ToggleViaInGameMap = ConfigurationReader.ReadBoolean("ToggleViaInGameMap");
            Size = ConfigurationReader.ReadInt16("Size");
            Position = ConfigurationReader.ParseEnum<MapPosition>("MapPosition");
            PrefetchAreas = Utils.ParseCommaSeparatedAreasByName(ConfigurationReader.ReadString("PrefetchAreas"));
            HiddenAreas = Utils.ParseCommaSeparatedAreasByName(ConfigurationReader.ReadString("HiddenAreas"));
            WarnImmuneNPC = Utils.ParseCommaSeparatedNpcsByName(ConfigurationReader.ReadString("WarnNPCImmune"));
            WarnImmuneNPCFontSize = ConfigurationReader.ReadInt32("WarnNPCFontSize");
            WarnImmuneNPCFont = ConfigurationReader.ReadString("WarnNPCFont");
            WarnNPCVerticalAlign = ConfigurationReader.ParseEnum<StringAlignment>("WarnNPCVerticalAlign");
            WarnNPCHorizontalAlign = ConfigurationReader.ParseEnum<StringAlignment>("WarnNPCHorizontalAlign");
            WarnNPCFontColor = Utils.ParseColor(ConfigurationReader.ReadString("WarnNPCFontColor"));
            ClearPrefetchedOnAreaChange = ConfigurationReader.ReadBoolean("ClearPrefetchedOnAreaChange");
        }
    }

    public class MapColorConfiguration : ConfigurationBase
    {
        public MapColorConfiguration()
        {
            ReadFromConfiguration();
        }

        readonly Dictionary<int, Color?> _mapColors = new Dictionary<int, Color?>();

        public void InitMapColors()
        {
            for (var i = -1; i < 600; i++)
            {
                LookupMapColor(i);
            }
        }

        public Color? LookupMapColor(int type)
        {
            var key = "MapColor[" + type + "]";

            if (!_mapColors.ContainsKey(type))
            {
                var mapColorString = ConfigurationReader.ReadString(key);
                if (!string.IsNullOrEmpty(mapColorString))
                {
                    _mapColors[type] = Utils.ParseColor(mapColorString);
                }
                else
                {
                    _mapColors[type] = null;
                }
            }

            return _mapColors[type];
        }

        protected override void ReadFromConfiguration()
        {
            InitMapColors();
        }
    }

    public class ApiConfiguration : ConfigurationBase
    {
        public ApiConfiguration()
        {
            ReadFromConfiguration();
        }
        public string Endpoint { get; set; }
        public string Token { get; set; }

        protected override void ReadFromConfiguration()
        {
            Endpoint = ConfigurationReader.ReadString("ApiEndpoint");
            Token = ConfigurationReader.ReadString("ApiToken");
        }
    }

    public class OffsetConfiguration : ConfigurationBase
    {
        public OffsetConfiguration()
        {
            ReadFromConfiguration();
        }
        public int UnitHashTable { get; set; }
        public int UiSettings { get; set; }
        public int ExpansionCheck { get; set; }

        protected override void ReadFromConfiguration()
        {
            UnitHashTable = ConfigurationReader.ReadInt32("UnitHashTable", 16);
            UiSettings = ConfigurationReader.ReadInt32("UiSettings", 16);
            ExpansionCheck = ConfigurationReader.ReadInt32("ExpansionCheck", 16);
        }
    }

    public class MapAssistConfiguration : ConfigurationBase
    {
        public ApiConfiguration Api { get; private set; }
        public MapColorConfiguration MapColors { get; private set; }
        public MapConfiguration Map { get; private set; }
        public RenderingConfiguration Rendering { get; private set; }
        public OffsetConfiguration Offsets { get; private set; }

        public MapAssistConfiguration()
        {
            ReadFromConfiguration();
        }
        protected override void ReadFromConfiguration()
        {
            Api = new ApiConfiguration();
            MapColors = new MapColorConfiguration();
            Map = new MapConfiguration();
            Rendering = new RenderingConfiguration();
            Offsets = new OffsetConfiguration();
        }
    }
}
