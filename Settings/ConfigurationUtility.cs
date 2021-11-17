﻿/**
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
using System.Globalization;
using System.Linq;
using MapAssist.Types;

// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace MapAssist.Settings
{
    public class ConfigurationReadException : Exception
    {
        public ConfigurationReadException(string parameterName, string type ,string optionalInfo = "") : base($"Error in parsing configuration! {parameterName} could not be parsed as {type}. {optionalInfo}")
        {
        }
    }
    /// <summary>
    /// Reads the configuration in a uniform way
    /// </summary>
    public static class ConfigurationReader
    {
        public static string ReadString(string configurationParameter)
        {
            string result = "";

            try
            {
                result = ConfigurationManager.AppSettings[configurationParameter];
            } 
            catch(Exception)
            {
                throw new ConfigurationReadException(configurationParameter, result.GetType().ToString());
            }

            return result;
        }
        public static bool ReadBoolean(string configurationParameter)
        {
            bool result = false;
            
            try 
            { 
                result = Convert.ToBoolean(ConfigurationManager.AppSettings[configurationParameter]);
            }
            catch(Exception)
            {
                throw new ConfigurationReadException(configurationParameter, result.GetType().ToString());
            }

            return result;
        }

        public static short ReadInt16(string configurationParameter)
        {
            short result = 0;

            try
            {
                result = Convert.ToInt16(ConfigurationManager.AppSettings[configurationParameter]);
            }
            catch (Exception)
            {
                throw new ConfigurationReadException(configurationParameter, result.GetType().ToString());
            }

            return result;
        }

        public static int ReadInt32(string configurationParameter, int? optionalBase = null)
        {
            int result = 0;

            try 
            { 
                if(optionalBase == null) 
                { 
                    result = Convert.ToInt32(ConfigurationManager.AppSettings[configurationParameter]);
                } 
                else
                {
                    result = Convert.ToInt32(ConfigurationManager.AppSettings[configurationParameter], optionalBase.Value);
                }
            }
            catch (Exception)
            {
                throw new ConfigurationReadException(configurationParameter, result.GetType().ToString());
            }

            return result;
        }

        public static char ReadChar(string configurationParameter)
        {
            char result = 'a';
            
            try
            {
                result = Convert.ToChar(ConfigurationManager.AppSettings[configurationParameter]);
            }
            catch (Exception)
            {
                throw new ConfigurationReadException(configurationParameter, result.GetType().ToString());
            }

            return result;
        }

        public static float ReadSingle(string configurationParameter)
        {
            float result = 0f;

            try
            {
                result = Convert.ToSingle(ConfigurationManager.AppSettings[configurationParameter], CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                throw new ConfigurationReadException(configurationParameter, result.GetType().ToString());
            }

            return result;
        }

        public static double ReadDouble(string configurationParameter)
        {
            double result = 0d;
            
            try
            {
                result = Convert.ToDouble(ConfigurationManager.AppSettings[configurationParameter], CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                throw new ConfigurationReadException(configurationParameter, result.GetType().ToString());
            }

            return result;
        }

        public static T ParseEnum<T>(string configurationParameter)
        {
            if (!typeof(T).IsEnum)
            {
                throw new ConfigurationReadException(configurationParameter, typeof(T).ToString());
            }
            
            try
            {
                T value = (T)Enum.Parse(typeof(T), ConfigurationManager.AppSettings[configurationParameter], true);
                return value;
            }
            catch (Exception)
            {
                var enumValues = Enum.GetValues(typeof(T));
                string enumValuesString = "";
                foreach(var value in  enumValues)
                {
                    enumValuesString += $"{value} ";
                }
                throw new ConfigurationReadException(configurationParameter, typeof(T).ToString(), $"Valid values are: {enumValuesString}");
            }

        }
    }
}
