﻿using OpenDreamServer.Dream;
using System;
using System.IO;
using System.Text;

namespace OpenDreamServer.Resources {
    class DreamResource {
        public string ResourcePath;
        public byte[] ResourceData {
            get {
                if (_resourceData == null && File.Exists(_filePath)) {
                    _resourceData = File.ReadAllBytes(_filePath);
                }

                return _resourceData;
            }
        }

        private string _filePath;
        private byte[] _resourceData = null;

        public DreamResource(string filePath, string resourcePath) {
            _filePath = filePath;
            ResourcePath = resourcePath;
        }

        public virtual string ReadAsString() {
            if (!File.Exists(_filePath)) return null;

            string resourceString = Encoding.ASCII.GetString(ResourceData);

            resourceString = resourceString.Replace("\r\n", "\n");
            return resourceString;
        }

        public virtual void Output(DreamValue value) {
            if (value.TryGetValueAsString(out string text)) {
                string filePath = Path.Combine(Program.DreamResourceManager.RootPath, ResourcePath);

                Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
                File.AppendAllText(filePath, text + "\r\n");
                _resourceData = null;
            } else {
                throw new Exception("Invalid output operation on '" + ResourcePath + "'");
            }
        }
    }
}
