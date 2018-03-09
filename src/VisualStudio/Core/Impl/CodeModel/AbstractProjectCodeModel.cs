﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    internal abstract class AbstractProjectCodeModel
    {
        private readonly NonReentrantLock _guard = new NonReentrantLock();
                
        private CodeModelProjectCache _codeModelCache;

        public AbstractProjectCodeModel(AbstractProject project, VisualStudioWorkspaceImpl visualStudioWorkspace, IServiceProvider serviceProvider)
        {
            VSProject = project;
            VisualStudioWorkspace = visualStudioWorkspace;
            ServiceProvider = serviceProvider;
        }

        protected AbstractProject VSProject { get; }
        protected VisualStudioWorkspaceImpl VisualStudioWorkspace { get; }
        protected IServiceProvider ServiceProvider { get; }

        internal void OnProjectClosed()
        {
            _codeModelCache?.OnProjectClosed();
        }

        private CodeModelProjectCache GetCodeModelCache()
        {
            Contract.ThrowIfNull(VSProject);
            Contract.ThrowIfNull(VisualStudioWorkspace);

            using (_guard.DisposableWait())
            {
                if (_codeModelCache == null)
                {
                    var project = VisualStudioWorkspace.CurrentSolution.GetProject(VSProject.Id);
                    if (project == null && !VSProject.PushingChangesToWorkspace)
                    {
                        // if this project hasn't been pushed yet, push it now so that the user gets a useful experience here.
                        VSProject.StartPushingToWorkspaceAndNotifyOfOpenDocuments();

                        // re-check to see whether we now has the project in the workspace
                        project = VisualStudioWorkspace.CurrentSolution.GetProject(VSProject.Id);
                    }

                    if (project != null)
                    {
                        _codeModelCache = new CodeModelProjectCache(VSProject, ServiceProvider, project.LanguageServices, VisualStudioWorkspace);
                    }
                }

                return _codeModelCache;
            }
        }

        public IEnumerable<ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>> GetCachedFileCodeModelInstances()
        {
            return GetCodeModelCache().GetFileCodeModelInstances();
        }

        public bool TryGetCachedFileCodeModel(string fileName, out ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel> fileCodeModelHandle)
        {
            var handle = GetCodeModelCache()?.GetComHandleForFileCodeModel(fileName);

            fileCodeModelHandle = handle != null
                ? handle.Value
                : default(ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>);

            return handle != null;
        }

        /// <summary>
        /// Gets or creates a <see cref="FileCodeModel"/> for the given file name. Because we don't have
        /// a parent object, this will call back to the project system to provide us the parent object.
        /// </summary>
        public ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel> GetOrCreateFileCodeModel(string filePath)
        {
            return GetCodeModelCache().GetOrCreateFileCodeModel(filePath);
        }

        public ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel> GetOrCreateFileCodeModel(string filePath, object parent)
        {
            return GetCodeModelCache().GetOrCreateFileCodeModel(filePath, parent);
        }

        public EnvDTE.CodeModel GetOrCreateRootCodeModel(EnvDTE.Project parent)
        {
            return GetCodeModelCache().GetOrCreateRootCodeModel(parent);
        }

        public void OnSourceFileRemoved(string fileName)
        {
            GetCodeModelCache().OnSourceFileRemoved(fileName);
        }

        public void OnSourceFileRenaming(string filePath, string newFilePath)
        {
            GetCodeModelCache().OnSourceFileRenaming(filePath, newFilePath);
        }

        internal abstract bool CanCreateFileCodeModelThroughProject(string filePath);
        internal abstract object CreateFileCodeModelThroughProject(string filePath);

    }
}
