// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SonarSmartTaggerProvider.cs" company="Copyright � 2013 Tekla Corporation. Tekla is a Trimble Company">
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

namespace VSSonarExtension.SmartTags.ContextMenu
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;

    using Microsoft.VisualStudio.Language.Intellisense;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Utilities;

    using VSSonarExtension.SmartTags.BufferUpdate;

    /// <summary>
    /// The spell smart tagger provider.
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("text")]
    [TagType(typeof(SmartTag))]
    public class SonarSmartTaggerProvider : IViewTaggerProvider
    {
        /// <summary>
        /// The all spelling tags.
        /// </summary>
        public static readonly Dictionary<string, SonarSmartTagger> AllSpellingTags = new Dictionary<string, SonarSmartTagger>();

        /// <summary>
        /// The tag aggregator factory.
        /// </summary>
        [Import]
        private IBufferTagAggregatorFactoryService tagAggregatorFactory;

        /// <summary>
        /// Gets or sets the tag aggregator factory.
        /// </summary>
        public IBufferTagAggregatorFactoryService TagAggregatorFactory
        {
            get { return this.tagAggregatorFactory; }
            set { this.tagAggregatorFactory = value; }
        }

        /// <summary>
        /// The create tagger.
        /// </summary>
        /// <param name="textView">
        /// The text view.
        /// </param>
        /// <param name="buffer">
        /// The thebuffer.
        /// </param>
        /// <typeparam name="T">
        /// the tag parameter
        /// </typeparam>
        /// <returns>
        /// The Microsoft.VisualStudio.Text.Tagging.ITagger`1[T -&gt; T].
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// argument null exception
        /// </exception>
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (textView == null)
            {
                throw new ArgumentNullException("textView");
            }

            if (buffer != textView.TextBuffer)
            {
                return null;
            }

            SonarSmartTagger sonartaginstance = null;

            var document = BufferTagger.GetPropertyFromBuffer<ITextDocument>(buffer);

            if (!AllSpellingTags.ContainsKey(document.FilePath))
            {
                sonartaginstance = new SonarSmartTagger(buffer, textView, true, document.FilePath);
                AllSpellingTags.Add(document.FilePath, sonartaginstance);
            }
            else
            {
                sonartaginstance = new SonarSmartTagger(buffer, textView, false, document.FilePath);
            }

            return (ITagger<T>)buffer.Properties.GetOrCreateSingletonProperty(() => sonartaginstance);
        }
    }
}