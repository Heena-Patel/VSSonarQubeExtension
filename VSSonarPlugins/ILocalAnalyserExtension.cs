﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ILocalAnalyserExtension.cs" company="Copyright © 2013 Tekla Corporation. Tekla is a Trimble Company">
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

namespace VSSonarPlugins
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Windows.Documents;

    using ExtensionTypes;

    /// <summary>
    /// The Sensors interface.
    /// </summary>
    public interface ILocalAnalyserExtension
    {
        /// <summary>
        /// The model will register to all extensions, so after analysis each
        /// extension will need to trigger this event to tell the model that issues
        /// are ready to read.
        /// Analysis will be done in a separate thread, therefore this is mandatory
        /// to be done for the extension to work
        /// </summary>
        event EventHandler LocalAnalysisCompleted;

        /// <summary>
        /// The model will spawned this method into a own thread, and read the results when
        /// LocalAnalysisCompleted event is triggered.
        /// </summary>
        /// <param name="filePath">
        /// The file Path.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        Thread GetFileAnalyserThread(string filePath);

        /// <summary>
        /// The get incremental analyser thread.
        /// </summary>
        /// <returns>
        /// The <see cref="Thread"/>.
        /// </returns>
        Thread GetIncrementalAnalyserThread();

        /// <summary>
        /// The get preview analyser thread.
        /// </summary>
        /// <returns>
        /// The <see cref="Thread"/>.
        /// </returns>
        Thread GetPreviewAnalyserThread();

        /// <summary>
        /// The get analyser thread.
        /// </summary>
        /// <returns>
        /// The <see cref="Thread"/>.
        /// </returns>
        Thread GetAnalyserThread();

        /// <summary>
        /// The get issues.
        /// </summary>
        /// <returns>
        /// The <see cref="List"/>.
        /// </returns>
        List<Issue> GetIssues();
    }

    /// <summary>
    /// The model expectes this argument to be sent refering the plugin key
    /// </summary>
    public class LocalAnalysisCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// The key.
        /// </summary>
        public readonly string Key;

        /// <summary>
        /// The error message.
        /// </summary>
        public readonly string ErrorMessage;

        /// <summary>
        /// The ex.
        /// </summary>
        public readonly Exception Ex;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalAnalysisCompletedEventArgs"/> class.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="errorMessage">
        /// The error Message.
        /// </param>
        /// <param name="ex">
        /// The ex.
        /// </param>
        public LocalAnalysisCompletedEventArgs(string key, string errorMessage, Exception ex)
        {
            this.Key = key;
            this.Ex = ex;
            this.ErrorMessage = errorMessage;
        }
    }
}