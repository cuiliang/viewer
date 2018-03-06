﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    public class MemoryAttributeStorage : IAttributeStorage
    {
        private Dictionary<string, AttributeCollection> _files = new Dictionary<string, AttributeCollection>();

        public IEnumerable<AttributeCollection> Files
        {
            get
            {
                foreach (var pair in _files)
                {
                    yield return pair.Value;
                }
            }
        }

        public AttributeCollection Load(string path)
        {
            if (!_files.TryGetValue(path, out AttributeCollection collection))
            {
                return new AttributeCollection(path, DateTime.Now, DateTime.Now);
            }

            return collection;
        }

        public void Store(AttributeCollection attrs)
        {
            if (_files.ContainsKey(attrs.Path))
            {
                _files[attrs.Path] = attrs;
            }
            else
            {
                _files.Add(attrs.Path, attrs);
            }
        }

        public void Flush()
        {
        }
    }
}