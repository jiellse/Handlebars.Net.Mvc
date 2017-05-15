using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.Hosting;

// Usage:
//	var vpp = new VPP(
//		new VPP.Dir("Views",
//			new VPP.Dir("_Layouts",
//				new VPP.File("default.hbs", "<html>{{{body}}}</html")
//			),
//			new VPP.File("index.hbs", "{{!< default}}This is the body.")
//		)
//	);
//
// (Would be nice if it could be as concise as Javascript notation.)

namespace HandlebarsDotNet.Mvc.Tests.TestInternal
{
	public class VPP : VirtualPathProvider
	{
		private Dir _root;

		public VPP(params Entry[] entries)
		{
			_root = new Dir("/", entries);
		}

		public override bool FileExists(string virtualPath)
		{
			var entry = GetEntry(virtualPath);
			return (entry != null) && (entry is VPP.File);
		}

		public override bool DirectoryExists(string virtualDir)
		{
			var entry = GetEntry(virtualDir);
			return (entry != null) && (entry is VPP.Dir);
		}

		public override VirtualFile GetFile(string virtualPath)
		{
			var entry = GetEntry(virtualPath);

			if(entry != null && entry is VPP.File)
			{
				var vf = new VPPFile(virtualPath, entry as VPP.File);

				return vf;
			}

			return null;
		}

		public override VirtualDirectory GetDirectory(string virtualDir)
		{
			var entry = GetEntry(virtualDir);

			if(entry != null && entry is VPP.Dir)
			{
				var dirpath = VirtualPathUtility.ToAbsolute(virtualDir, "/");

				var vd = new VPPDir(dirpath, entry as VPP.Dir);

				return vd;
			}

			return null;
		}

		public override string GetCacheKey(string virtualPath)
		{
			return base.GetCacheKey(virtualPath);
		}

		public override string GetFileHash(string virtualPath, IEnumerable virtualPathDependencies)
		{
			return base.GetFileHash(virtualPath, virtualPathDependencies);
		}

		public override CacheDependency GetCacheDependency(string virtualPath, IEnumerable virtualPathDependencies, DateTime utcStart)
		{
			return null;
		}

		[System.Diagnostics.DebuggerStepThrough]
		private Entry GetEntry(string virtualPath)
		{
			var path = VirtualPathUtility.ToAbsolute(virtualPath, "/");

			Entry entry = _root;
			foreach(var part in path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
			{
				if(entry is Dir)
				{
					var dir = entry as Dir;

					entry = dir.Children.SingleOrDefault(e => e.Name == part);

					if(entry == null)
						return null;
				}
				else
					return null;
			}

			return entry;
		}

		private class VPPFile : VirtualFile
		{
			VPP.File _entry;

			public VPPFile(string virtualPath, VPP.File entry)
				: base(virtualPath)
			{
				_entry = entry;
			}

			public override Stream Open()
			{
				return new MemoryStream(Encoding.UTF8.GetBytes(_entry.Contents));
			}
		}

		private class VPPDir : VirtualDirectory
		{
			VPP.Dir _entry;

			public VPPDir(string virtualPath, VPP.Dir entry)
				: base(virtualPath)
			{
				_entry = entry;
			}

			public override IEnumerable Children
			{
				get
				{
					var children = new List<VirtualFileBase>();
					foreach(var child in _entry.Children)
					{
						string virpath = this.VirtualPath + child.Name;
						if(child is VPP.Dir)
							children.Add(new VPPDir(virpath, child as Dir));
						else if(child is VPP.File)
							children.Add(new VPPFile(virpath, child as File));
						else
							throw new Exception("VPP: Not directory or file??");
					}
					return children;
				}
			}

			public override IEnumerable Directories
			{
				get
				{
					var dirs = new List<VirtualDirectory>();
					foreach(var child in _entry.Children)
					{
						string virpath = this.VirtualPath + child.Name;
						if(child is VPP.Dir)
							dirs.Add(new VPPDir(virpath, child as Dir));
					}
					return dirs;
				}
			}

			public override IEnumerable Files
			{
				get
				{
					var files = new List<VirtualFile>();
					foreach(var child in _entry.Children)
					{
						string virpath = this.VirtualPath + child.Name;
						if(child is VPP.File)
							files.Add(new VPPFile(virpath, child as File));
					}
					return files;
				}
			}
		}

		//---------------------------------------------------------------------------------------------

		public abstract class Entry
		{
			public Entry(string name)
			{
				Name = name;
			}

			public string Name
			{
				get;
				set;
			}

			public Entry Parent		// the directory that has this file/dir. If null it is at top-level
			{
				get;
				set;
			}
		}

		public class Dir : Entry
		{
			private List<Entry> _entries;

			public Dir(string name, params Entry[] entries)
				: base(name)
			{
				_entries = new List<Entry>(entries);

				_entries.ForEach(e => e.Parent = this);
			}

			public List<Entry> Children
			{
				get
				{
					return _entries;
				}
			}
		}

		public class File : Entry
		{
			public File(string name, string contents)
				: base(name)
			{
				Contents = contents;
			}

			public string Contents
			{
				get;
				set;
			}
		}
	}
}
