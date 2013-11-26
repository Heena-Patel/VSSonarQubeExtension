﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VSSonarQubeExtensionPackage.cs" company="Copyright © 2013 Tekla Corporation. Tekla is a Trimble Company">
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

namespace VSSonarExtension.PackageImplementation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;

    using EnvDTE;

    using EnvDTE80;

    using ExtensionHelpers;

    using ExtensionTypes;

    using ExtensionViewModel.ViewModel;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using SonarRestService;

    using VSSonarExtension.PckResources;
    using VSSonarExtension.VSControls;
    using VSSonarExtension.VSControls.DialogOptions;

    using VSSonarPlugins;

    using MessageBox = System.Windows.Forms.MessageBox;
    using Thread = System.Threading.Thread;

    /// <summary>
    ///     The vs sonar extension package.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(SonarGeneralOptionsPage), "Sonar Options", "General", 1000, 1001, true)]
    [ProvideProfile(typeof(SonarGeneralOptionsPage), "Sonar Options", "General", 1002, 1003, true)]
    [ProvideAutoLoad("{05ca2046-1eb1-4813-a91f-a69df9b27698}")]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [ProvideToolWindow(typeof(IssuesToolWindow), Style = VsDockStyle.Tabbed, 
        Window = "ac305e7a-a44b-4541-8ece-3c88d5425338")]
    [Guid(GuidList.GuidVSSonarExtensionPkgString)]
    public sealed partial class VsSonarExtensionPackage : Package
    {
        #region Static Fields

        /// <summary>
        ///     The issue model.
        /// </summary>
        public static readonly ExtensionDataModel ExtensionModelData = new ExtensionDataModel();

        /// <summary>
        /// The plugin control.
        /// </summary>
        public static readonly PluginController PluginControl  = new PluginController();

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="VsSonarExtensionPackage" /> class.
        /// </summary>
        public VsSonarExtensionPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this));
        }

        #endregion

        #region Methods

        /// <summary>
        ///     The initialize.
        /// </summary>
        protected override void Initialize()
        {
            try
            {
                Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this));
                base.Initialize();

                this.SetupMenuCommands();
                this.dte2 = (DTE2)GetGlobalService(typeof(DTE));

                if (this.dte2 == null)
                {
                    return;
                }

                this.visualStudioInterface = new VsPropertiesHelper(this.dte2);
                this.restService = new SonarRestService(new JsonSonarConnector());

                // int configuration options
                this.InitOptions();

                try
                {
                    ExtensionModelData.Vsenvironmenthelper = this.visualStudioInterface;
                    ExtensionModelData.RestService = this.restService;
                    ExtensionModelData.PluginController = PluginControl;
                    this.UpdateModelInToolWindow(ExtensionModelData);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("SonarQubeExtension is not usable condition, check credential and host from tools > options > Sonar, and restart visual studio after settings are set : ", ex.StackTrace);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Resources.VSEXTENSIONFAILEDINIT + ex.Message + " " + ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// The analysis controller.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void AnalysisController(object sender, EventArgs e)
        {
            if (sender == null)
            {
                return;
            }

            var command = sender as OleMenuCommand;

            if (!this.sem.WaitOne(0))
            {
                return;
            }

            try
            {
                this.UpdateServerButton(command);
                this.UpdateLocalButton(command);
                this.UpdateLocalUser(command);
            }
            catch (Exception ex)
            {
                ExtensionModelData.ErrorMessage = "Error";
                ExtensionModelData.DiagnosticMessage = ex.Message + "\r\n" + ex.StackTrace;
            }

            this.sem.Release();
        }

        /// <summary>
        /// The get coverage menu item callback.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void CallbackGetCoverageMenuItem(object sender, EventArgs e)
        {
            if (sender == null)
            {
                return;
            }

            var command = sender as OleMenuCommand;
            if (command == null)
            {
                return;
            }

            if (command.Checked)
            {
                this.sonarCoverageMenuCommand.Checked = false;
                this.sonarCoverageCommandBar.Checked = false;
                ExtensionModelData.EnableCoverageInEditor = false;
            }
            else
            {
                this.sonarCoverageMenuCommand.Checked = true;
                this.sonarCoverageCommandBar.Checked = true;
                (new Thread(() => { ExtensionModelData.EnableCoverageInEditor = true; })).Start();
            }
        }

        /// <summary>
        /// The get source diff menu item callback.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void CallbackGetSourceDiffMenuItem(object sender, EventArgs e)
        {
            ExtensionModelData.DisplayDiferenceToServerSource();
        }

        /// <summary>
        ///     The init general options.
        /// </summary>
        private void InitGeneralOptions()
        {
            this.visualStudioInterface.WriteDefaultOption(
                "Sonar Options", "General", "SonarHost", "http://localhost:9000");
            this.visualStudioInterface.WriteDefaultOption("Sonar Options", "General", "DisableEditorTags", "FALSE");
        }

        /// <summary>
        ///     The init options.
        /// </summary>
        private void InitOptions()
        {
            this.InitGeneralOptions();

            ExtensionDataModel.PluginsOptionsData.Plugins = PluginControl.GetPlugins();
            ExtensionDataModel.PluginsOptionsData.Vsenvironmenthelper = this.visualStudioInterface;
            ExtensionDataModel.PluginsOptionsData.ResetUserData();
            if (ExtensionDataModel.PluginsOptionsData.Plugins == null)
            {
                return;
            }

            foreach (var plugin in ExtensionDataModel.PluginsOptionsData.Plugins)
            {
                if (plugin.GetUsePluginControlOptions() == null)
                {
                    continue;
                }

                plugin.GetUsePluginControlOptions().SetOptions(this.visualStudioInterface.ReadAllOptionsForPluginOptionInApplicationData(plugin.GetKey()));
            }
        }

        /// <summary>
        ///     The setup menu commands.
        /// </summary>
        private void SetupMenuCommands()
        {
            var mcs = this.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            if (null == mcs)
            {
                return;
            }

            // menu commands 
            var menuCommandId = new CommandID(
                GuidList.GuidVSSonarExtensionCmdSet, (int)PkgCmdIdList.CmdidSonarServerViolationsCommand);
            this.sonarViolationMenuCommand = new OleMenuCommand(this.AnalysisController, menuCommandId);
            mcs.AddCommand(this.sonarViolationMenuCommand);

            menuCommandId = new CommandID(
                GuidList.GuidVSSonarExtensionCmdSet, (int)PkgCmdIdList.CmdidLocalAnalysisCommand);
            this.sonarLocalAnalysisMenuCommand = new OleMenuCommand(this.AnalysisController, menuCommandId);
            mcs.AddCommand(this.sonarLocalAnalysisMenuCommand);

            menuCommandId = new CommandID(
                GuidList.GuidVSSonarExtensionCmdSet, (int)PkgCmdIdList.CmdidLocalViolationsFromChangesCommand);
            this.sonarLocalAnalysisAddedMenuCommand = new OleMenuCommand(this.AnalysisController, menuCommandId);
            mcs.AddCommand(this.sonarLocalAnalysisAddedMenuCommand);

            menuCommandId = new CommandID(GuidList.GuidVSSonarExtensionCmdSet, (int)PkgCmdIdList.CmdidCoverageCommand);
            this.sonarCoverageMenuCommand = new OleMenuCommand(this.CallbackGetCoverageMenuItem, menuCommandId);
            mcs.AddCommand(this.sonarCoverageMenuCommand);

            menuCommandId = new CommandID(GuidList.GuidVSSonarExtensionCmdSet, (int)PkgCmdIdList.CmdidSourceDiffCommand);
            this.sonarSourceDiffMenuCommand = new OleMenuCommand(this.CallbackGetSourceDiffMenuItem, menuCommandId);
            mcs.AddCommand(this.sonarSourceDiffMenuCommand);

            menuCommandId = new CommandID(GuidList.GuidVSSonarExtensionCmdSet, (int)PkgCmdIdList.CmdidReviewsCommand);
            this.sonarReviewsCommand = new OleMenuCommand(this.ShowIssuesToolWindow, menuCommandId);
            mcs.AddCommand(this.sonarReviewsCommand);

            // toolbar menus commands
            menuCommandId = new CommandID(
                GuidList.GuidShowInitialToolbarCmdSet, (int)PkgCmdIdList.ToolBarReportSonarViolations);
            this.sonarViolationCommandBar = new OleMenuCommand(this.AnalysisController, menuCommandId);
            mcs.AddCommand(this.sonarViolationCommandBar);

            menuCommandId = new CommandID(
                GuidList.GuidShowInitialToolbarCmdSet, (int)PkgCmdIdList.ToolBarReportLocalViolations);
            this.sonarLocalAnalysisCommandBar = new OleMenuCommand(this.AnalysisController, menuCommandId);
            mcs.AddCommand(this.sonarLocalAnalysisCommandBar);

            menuCommandId = new CommandID(
                GuidList.GuidShowInitialToolbarCmdSet, (int)PkgCmdIdList.ToolBarReportLocalAddViolations);
            this.sonarLocalAnalysisAddedCommandBar = new OleMenuCommand(this.AnalysisController, menuCommandId);
            mcs.AddCommand(this.sonarLocalAnalysisAddedCommandBar);

            menuCommandId = new CommandID(
                GuidList.GuidShowInitialToolbarCmdSet, (int)PkgCmdIdList.ToolBarReportCoverage);
            this.sonarCoverageCommandBar = new OleMenuCommand(this.CallbackGetCoverageMenuItem, menuCommandId);
            mcs.AddCommand(this.sonarCoverageCommandBar);

            menuCommandId = new CommandID(GuidList.GuidShowInitialToolbarCmdSet, (int)PkgCmdIdList.ToolBarReportSource);
            this.sonarSourceDiffCommandBar = new OleMenuCommand(this.CallbackGetSourceDiffMenuItem, menuCommandId);
            mcs.AddCommand(this.sonarSourceDiffCommandBar);

            menuCommandId = new CommandID(GuidList.GuidShowInitialToolbarCmdSet, (int)PkgCmdIdList.ToolBarReportReviews);
            this.sonarReviewsCommandBar = new OleMenuCommand(this.ShowIssuesToolWindow, menuCommandId);
            mcs.AddCommand(this.sonarReviewsCommandBar);
        }

        /// <summary>
        /// The show issues tool window.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        /// <exception>
        /// <cref>NotSupportedException</cref>
        /// </exception>
        private void ShowIssuesToolWindow(object sender, EventArgs e)
        {
            IVsWindowFrame windowFrame;

            using (var window = this.FindToolWindow(typeof(IssuesToolWindow), 0, true))
            {
                if ((null == window) || (null == window.Frame))
                {
                    throw new NotSupportedException("Window Not Found");
                }

                windowFrame = (IVsWindowFrame)window.Frame;
            }

            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        /// <summary>
        /// The update local button.
        /// </summary>
        /// <param name="command">
        /// The command.
        /// </param>
        private void UpdateLocalButton(MenuCommand command)
        {
            if (command != this.sonarLocalAnalysisMenuCommand && command != this.sonarLocalAnalysisCommandBar)
            {
                return;
            }

            if (command.Checked)
            {
                this.sonarLocalAnalysisMenuCommand.Checked = false;
                this.sonarLocalAnalysisCommandBar.Checked = false;
                this.sonarLocalAnalysisAddedMenuCommand.Checked = false;
                this.sonarLocalAnalysisAddedCommandBar.Checked = false;
                ExtensionModelData.ServerDeprecatedAnalysis = ExtensionDataModel.DeprecatedAnalysesType.Off;
                ExtensionModelData.IssuesInEditor = new List<Issue>();
                if (!ExtensionModelData.IssuesInViewLocked)
                {
                    ExtensionModelData.Issues = new List<Issue>();
                }
            }
            else
            {
                this.sonarLocalAnalysisMenuCommand.Checked = true;
                this.sonarLocalAnalysisCommandBar.Checked = true;
                this.sonarViolationMenuCommand.Checked = false;
                this.sonarViolationCommandBar.Checked = false;

                ExtensionModelData.ServerDeprecatedAnalysis = ExtensionDataModel.DeprecatedAnalysesType.Local;
                string filePath = this.visualStudioInterface.ActiveFileFullPath();
                string refsource = string.Join<string>("\r\n", File.ReadAllLines(filePath));
                ExtensionModelData.LastReferenceSource = refsource;
                ExtensionModelData.UpdateDataInEditor();
            }
        }

        /// <summary>
        /// The update local user.
        /// </summary>
        /// <param name="command">
        /// The command.
        /// </param>
        private void UpdateLocalUser(MenuCommand command)
        {
            if (command != this.sonarLocalAnalysisAddedMenuCommand && command != this.sonarLocalAnalysisAddedCommandBar)
            {
                return;
            }

            if (command.Checked)
            {
                this.sonarLocalAnalysisAddedMenuCommand.Checked = false;
                this.sonarLocalAnalysisAddedCommandBar.Checked = false;
                ExtensionModelData.ServerDeprecatedAnalysis = ExtensionDataModel.DeprecatedAnalysesType.Local;
                string filePath = this.visualStudioInterface.ActiveFileFullPath();
                string refsource = string.Join<string>("\r\n", File.ReadAllLines(filePath));
                ExtensionModelData.LastReferenceSource = refsource;
                ExtensionModelData.UpdateDataInEditor();
            }
            else
            {
                this.sonarLocalAnalysisMenuCommand.Checked = true;
                this.sonarLocalAnalysisCommandBar.Checked = true;
                this.sonarLocalAnalysisAddedMenuCommand.Checked = true;
                this.sonarLocalAnalysisAddedCommandBar.Checked = true;
                this.sonarViolationMenuCommand.Checked = false;
                this.sonarViolationCommandBar.Checked = false;

                ExtensionModelData.ServerDeprecatedAnalysis = ExtensionDataModel.DeprecatedAnalysesType.Localuser;
                string filePath = this.visualStudioInterface.ActiveFileFullPath();
                string refsource = string.Join<string>("\r\n", File.ReadAllLines(filePath));
                ExtensionModelData.LastReferenceSource = refsource;
                ExtensionModelData.UpdateDataInEditor();
            }
        }

        /// <summary>
        /// The update model in tool window.
        /// </summary>
        /// <param name="modelToUse">
        /// The model to use.
        /// </param>
        private void UpdateModelInToolWindow(ExtensionDataModel modelToUse)
        {
            // init basic model without project association
            ToolWindowPane window = this.FindToolWindow(typeof(IssuesToolWindow), 0, true) as IssuesToolWindow;

            if (null == window || modelToUse == null)
            {
                return;
            }

            var win = window as IssuesToolWindow;
            modelToUse.ExtensionDataModelUpdate(new SonarRestService(new JsonSonarConnector()), new VsPropertiesHelper(this.dte2), null);
            win.UpdateModel(modelToUse);
        }

        /// <summary>
        /// The update server button.
        /// </summary>
        /// <param name="command">
        /// The command.
        /// </param>
        private void UpdateServerButton(MenuCommand command)
        {
            if (command != this.sonarViolationMenuCommand && command != this.sonarViolationCommandBar)
            {
                return;
            }

            if (command.Checked)
            {
                this.sonarViolationMenuCommand.Checked = false;
                this.sonarViolationCommandBar.Checked = false;
                ExtensionModelData.ServerDeprecatedAnalysis = ExtensionDataModel.DeprecatedAnalysesType.Off;
                ExtensionModelData.IssuesInEditor = new List<Issue>();
                if (!ExtensionModelData.IssuesInViewLocked)
                {
                    ExtensionModelData.Issues = new List<Issue>();
                }
            }
            else
            {
                this.sonarViolationMenuCommand.Checked = true;
                this.sonarViolationCommandBar.Checked = true;
                this.sonarLocalAnalysisAddedCommandBar.Checked = false;
                this.sonarLocalAnalysisAddedMenuCommand.Checked = false;
                this.sonarLocalAnalysisCommandBar.Checked = false;
                this.sonarLocalAnalysisMenuCommand.Checked = false;
                (new Thread(
                    () =>
                        {
                            ExtensionModelData.ServerDeprecatedAnalysis = ExtensionDataModel.DeprecatedAnalysesType.Server;
                            ExtensionModelData.UpdateDataInEditor();
                        })).Start();
            }
        }

        #endregion
    }
}