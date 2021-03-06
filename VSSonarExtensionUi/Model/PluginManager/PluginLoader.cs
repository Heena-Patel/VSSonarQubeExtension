﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PluginLoader.cs" company="Copyright © 2014 Tekla Corporation. Tekla is a Trimble Company">
//     Copyright (C) 2013 [Jorge Costa, Jorge.Costa@tekla.com]
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
// This program is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License
// as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with this program; if not, write to the Free
// Software Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
// --------------------------------------------------------------------------------------------------------------------
namespace VSSonarExtensionUi.Model.PluginManager
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using VSSonarPlugins;

    /// <summary>
    ///     The local analyser.
    /// </summary>
    public class PluginLoader : IPluginLoader
    {
        /// <summary>
        ///     The error data.
        /// </summary>
        private string errorData;

        /// <summary>
        ///     The get error data.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public string GetErrorData()
        {
            return this.errorData;
        }

        public IPlugin LoadPlugin(Assembly assembly)
        {
            var types2 = assembly.GetTypes();
            foreach (var type in types2)
            {
                Debug.WriteLine("Type In Assembly:" + type.FullName);

                try
                {
                    if (typeof(IAnalysisPlugin).IsAssignableFrom(type))
                    {
                        Debug.WriteLine("Can Cast Type In Assembly To: " + typeof(IAnalysisPlugin).FullName);
                        return (IPlugin)Activator.CreateInstance(type);
                    }

                    if (typeof(IMenuCommandPlugin).IsAssignableFrom(type))
                    {
                        Debug.WriteLine("Can Cast Type In Assembly To: " + typeof(IMenuCommandPlugin).FullName);
                        return (IPlugin)Activator.CreateInstance(type);
                    }

                    if (typeof(ISourceVersionPlugin).IsAssignableFrom(type))
                    {
                        Debug.WriteLine("Can Cast Type In Assembly To: " + typeof(ISourceVersionPlugin).FullName);
                        return (IPlugin)Activator.CreateInstance(type);
                    }

                    if (typeof(IIssueTrackerPlugin).IsAssignableFrom(type))
                    {
                        Debug.WriteLine("Can Cast Type In Assembly To: " + typeof(IIssueTrackerPlugin).FullName);
                        return (IPlugin)Activator.CreateInstance(type);
                    }
                }
                catch (Exception ex)
                {
                    this.errorData += ex.Message;
                    Debug.WriteLine(ex.InnerException.Message + " : " + ex.InnerException.StackTrace);
                }
            }

            return null;
        }
    }
}