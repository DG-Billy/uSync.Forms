﻿using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Extensions;
using Umbraco.Forms.Core.Models;
using Umbraco.Forms.Core.Services;

namespace uSync.Forms.Services
{
    public partial class SyncFormService
    {
        private bool IsNew(Folder folder)
            => folder.Id == Guid.Empty || folderService.Get(folder.Id) == null;

        public void SaveFolder(Folder item)
        {
            _ = IsNew(item) ? folderService.Insert(item) : folderService.Update(item);
        }

        public void DeleteFolder(Folder item)
            => folderService.Delete(item);

        public IEnumerable<Folder> GetChildFolders(Guid? parent = null)
        {
            if (parent == null)
                return folderService.GetAtRoot();

            return folderService.GetChildren(parent.Value);
        }

        public IEnumerable<Folder> GetAllFolders(Guid? parent = null)
        {
            var folders = new List<Folder>();

            if (parent != null)
            {
                folders.AddRange(folderService.GetChildren(parent.Value));
            }
            else
            {
                folders.AddRange(folderService.GetAtRoot());
            }

            foreach (var folder in folders) {
                folders.AddRange(GetAllFolders(folder.Id));
            }

            return folders;
        }

        public Folder GetFolder(Guid folderId) 
        {
            try
            {
                return folderService.Get(folderId);
            }
            catch
            {
                return null;
            }
        }

        // return all the forms in a folder or its children.
        public IEnumerable<Form> GetFolderForms(Guid folderId)
        {
            var ids = new List<Guid>()
            {
                folderId
            };

            ids.AddRange(folderService.GetChildren(folderId)
                .Select(x => x.Id));

            // all forms who live in a folder that is either this folder or a child folder
            return formService.Get().Where(x => x.FolderId != null && ids.Contains(x.FolderId.Value));
        }

        public string GetFolderPath(Guid folderId)
        {
            var path = "";
            var folder = GetFolder(folderId);
            if (folder != null)
            {
                if (folder.ParentId != null)
                {
                    // has a parent. 
                    path = GetFolderPath(folder.ParentId.Value);
                }
            }

            path += "/" + folder.Name;

            return path;
        }

        public Folder CreateOrFindFolders(Guid parent, string folderPath)
        {
            return CreateOrFindFoldersInternal(parent, folderPath);
        }

        private Folder CreateOrFindFoldersInternal(Guid parent, string folderPath) 
        { 
            var folderPathClean = folderPath.Trim('/');

            IEnumerable<Folder> folders; 
            if (parent == Guid.Empty)
            {
                folders = folderService.GetAtRoot();
            }
            else
            {
                folders = folderService.GetChildren(parent);
            }

            var folder = folderPathClean;
            if(folderPathClean.IndexOf('/') != -1)
            {
                folder = folderPathClean.Substring(0, folderPathClean.IndexOf('/'));
            }

            var formFolder = folders.FirstOrDefault(x => x.Name.InvariantEquals(folder));

            if (formFolder == null) {

                formFolder = new Folder
                {
                    Name = folder,
                };

                if (parent != Guid.Empty) formFolder.ParentId = parent;

                try
                {
                    formFolder = ((IFolderService)folderService).Insert(formFolder);
                }
                catch(Exception ex)
                {
                    // error (could be we are importing to something that doesn't 
                    // support folders)
                    return null;
                }
            };

            if (folderPathClean.IndexOf('/') != -1)
            {
                var remaining = folderPathClean.Substring(folderPathClean.IndexOf('/'));
                return CreateOrFindFolders(formFolder.Id, remaining);
            }
            else
            {
                return formFolder;
            }
        }
    }
}
