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
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using MapAssist.Settings;

namespace MapAssist
{
    static class Program
    {
        private static MapAssistConfiguration ReadConfiguration()
        {
            try
            {
                return new MapAssistConfiguration();
            }
            catch (ConfigurationReadException e)
            {
                MessageBox.Show(e.Message, "Configuration parsing error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Configuration parsing error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            return null;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (IKeyboardMouseEvents globalHook = Hook.GlobalEvents())
            {
                MapAssistConfiguration mapAssistConfiguration = ReadConfiguration();

                if(mapAssistConfiguration != null) { 
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Overlay(globalHook, mapAssistConfiguration));
                }
            }
        }
    }
}
