using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.IO;
using iTunesLib;



namespace italo {

    public class LibraryScanner
    {
        private iTunesApp _library;
        private Logger _log;
        private IITTrackCollection _tracks;
        private int _trackCount;
        private List<string> _libraryLocations = new List<string>();
        private FileSystemWatcher _watcher;
        private Thread _scanThread = null;
        private Stack<string> _scanningDirectories = new Stack<string>();

        HashSet<string> supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".mp3", ".wma", ".mp4", ".wav", ".aac", ".m4a" };
        private bool _scanLoopRunning = false;
        private object _stackLock = new object();

        private List<string> _addedDirs = new List<string>();

        private int _numTasks = 0;
        private int _finishedTasks = 0;

        private void AddFileToLibrary(FileInfo info)
        {
            if (!FileExistsInLibrary(info))
            {
                _log.LogInfo("Adding file " + info.FullName + " to iTunes Library");
                //Prevent re-adding, Isn't really a problem but nice...
                _libraryLocations.Add(info.FullName);
                var result = _library.LibraryPlaylist.AddFile(info.FullName);

            }
            else
            {
                _log.LogDebug("File " + info.FullName + " already exists in iTunes Library");
            }
                /*if (result.Tracks != null && result.Tracks.Count > 0) {
                //var track = result.Tracks.get_ItemByName(info.FullName);
                foreach (IITTrack track in result.Tracks)
                {
                    if (track.Kind == ITTrackKind.ITTrackKindFile)
                    {
                        IITFileOrCDTrack fileTrack = (IITFileOrCDTrack)track;
                     //FIXME: THIS DESTROYS ID3-TAGS
                     //   SetTrackArt(fileTrack);
                     //   _log.LogInfo("adding albumart for track " + fileTrack.Name);
                    }
                }*/
        }

        private void SetTrackArt(IITFileOrCDTrack track)
        {
            if (track.Artwork.Count == 0 && !track.Podcast)
            {
                string fileLoc = System.IO.Path.GetDirectoryName(track.Location);
                string artPath = System.IO.Path.Combine(fileLoc, "folder.jpg");
                if (System.IO.File.Exists(artPath))
                {
                    _log.LogInfo("Adding art to " + track.Location);
                    try
                    {
                        track.AddArtworkFromFile(artPath);
                    }
                    catch
                    {
                        _log.LogError("Couldn't set albumart...");
                    }
                }
            }
        }

        private bool FileExistsInLibrary(FileInfo info)
        {
            var found = _libraryLocations.Find(x => x == info.FullName);

            if (found == null)
                return false;
            return true;
        }

        private bool TrackExistsInFileSystem(IITFileOrCDTrack fileTrack) {
            return System.IO.File.Exists(fileTrack.Location);
        }

        private List<string> FileList()
        {
            List<string> list = new List<string>();

            foreach (IITTrack currentTrack in _tracks)
            {
                if (currentTrack.Kind == ITTrackKind.ITTrackKindFile)
                {
                    IITFileOrCDTrack currentFileTrack = (IITFileOrCDTrack)currentTrack;

                    if (currentFileTrack.Location == String.Empty || !System.IO.File.Exists(currentFileTrack.Location))
                    {
                        _log.LogInfo("Dead track found: " + currentFileTrack.Name);
                        //list.Add(currentFileTrack.Location);
                        //fileTrack.Delete();
                    }

                    if (currentFileTrack.Location != String.Empty)
                    {
                        _log.LogDebug("Adding " + currentFileTrack.Location + " to locationlist");
                        list.Add(currentFileTrack.Location);
                        RemoveTask();
                    }
                }
            }
            
            _log.LogDebug("Created Location List");
            return list;
        }

        private void StartScanDirectory()
        {
            while (_scanningDirectories.Count != 0)
            {
                string directory;
                
                lock (_stackLock)
                {
                    directory = _scanningDirectories.Pop();

                    _log.LogDebug("Adding files from folder " + directory);

                    var result = (from file in Directory.EnumerateFiles(directory, @"*", SearchOption.TopDirectoryOnly).Where(s => supportedExtensions.Contains(Path.GetExtension(s)))
                                  select file);

                    foreach (var file in result)
                    {
                        FileInfo fInfo = new FileInfo(file);

                        _log.LogDebug("Adding file: " + file);
                        AddFileToLibrary(fInfo);
                        RemoveTask();
                    }
                }
            }
            SetScanEnd();
        }

        private void AddScanDirectory(string topdirectory)
        {
            lock (_stackLock)
            {
                if (AlreadyScheduledToScan(topdirectory))
                    return;

                _scanningDirectories.Push(topdirectory);
                _addedDirs.Add(topdirectory);

                var result = (from dir in Directory.EnumerateDirectories(topdirectory, @"*", SearchOption.AllDirectories)
                              select dir);

                foreach (var dir in result) 
                {
                    if (AlreadyScheduledToScan(dir))
                    {
                        _scanningDirectories.Push(dir);
                        _addedDirs.Add(dir);
                    }
                }
            }

            if (_scanLoopRunning == false)
            {
                _log.LogDebug("Scan NOT running so will start..!");
                _scanLoopRunning = true;
                StartScanDirectory();
            }
            else
            {
                _log.LogDebug("Scan running so won't start it...");
            }
        }

        private int CountFiles(string directory)
        {
            var fileCount = 0;

            try
            {
                fileCount = (from file in Directory.EnumerateFiles(directory, @"*", SearchOption.AllDirectories).Where(s => supportedExtensions.Contains(Path.GetExtension(s)))
                                 select file).Count();

                return fileCount;
            }
            catch
            {
                return fileCount;
            }
        }

        private void SetScanStart()
        {
            MainWindow.SetScanStart();
            _addedDirs = new List<string>();
        }

        private void SetScanEnd()
        {
            _scanLoopRunning = false;

            if (_addedDirs.Count == 1)
            {
                MainWindow.ShowNotify("Added folder " + _addedDirs[0] + " to iTunes");
            }
            else
            {
                MainWindow.ShowNotify("Added " + _addedDirs.Count() + " directories to iTunes");
            }

            _log.LogInfo("Scan finished");
            
            MainWindow.SetScanEnd();
            EndTasks();
        }

        private void FullScan(object parameters)
        {
            SetScanStart();

            object[] parameterArray = (object[])parameters;
            string directory = (string)parameterArray[0];
            bool full = (bool)parameterArray[1];
            int sleep = (int)parameterArray[2];

            Thread.Sleep(sleep);

            int numFiles = CountFiles(directory);

            if (full == true)
            {
                iTunesAppClass _iTunes = new iTunesAppClass();
                _tracks = _iTunes.LibraryPlaylist.Tracks;
                _trackCount = _tracks.Count;

                SetTasks(numFiles + _trackCount);

                _log.LogInfo("Starting full scan in " + directory);
                _libraryLocations = FileList();
            }
            else
            {
                _log.LogDebug("Starting partial scan in " + directory);

                AddTasks(numFiles);
            }

            AddScanDirectory(directory);
        }

        private bool AlreadyScheduledToScan(string directory)
        {
            if (_scanningDirectories.Contains(directory))
                return true;
            return false;
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            string path;

            if (Directory.Exists(e.FullPath))
                path = e.FullPath;
            else
                path = Path.GetDirectoryName(e.FullPath);

            SetScanStart();

            _log.LogDebug("Will rescan " + path + " due to: " + e.ChangeType);
            StartScan(path, false, 0);
        }

        /*FIXME: THIS IS LAZY*/

        private void SetProgress()
        {
            try
            {
                //Double num = (_finishedTasks / _numTasks);
                Double num = ((Double)_finishedTasks / (Double)_numTasks);
                num = num * 100;
                double he = Math.Round(num, 0);

                if (he < 0)
                    MainWindow.ProgressBarUpdate(0);
                else
                    MainWindow.ProgressBarUpdate((int)he);
            }
            catch
            {
                MainWindow.ProgressBarUpdate(0);
            }
        }

        private void SetTasks(int numTasks)
        {
            _numTasks = numTasks;
            SetProgress();
        }

        private void AddTasks(int numTasks)
        {
            _numTasks = numTasks + _numTasks;
            SetProgress();
        }

        private void RemoveTask()
        {
            _finishedTasks++;
            SetProgress();
        }

        private void EndTasks()
        {
            _numTasks = 0;
            _finishedTasks = 0;
            SetProgress();
        }

        /*public methods start here!!!*/

        public LibraryScanner(Logger logger)
        {
            if (logger == null)
                throw new ArgumentException("logger parameter is null.");
            _log = logger;
            //FIXME: Move
            _library = new iTunesApp();
        }

        public void StartScan(string directory, bool full, int sleep)

        {
            if (!Directory.Exists(directory))
            {
                System.Windows.MessageBox.Show("Search directory does not exist or is unaccessible");
            }
            else
            {
                _scanThread = new Thread(new ParameterizedThreadStart(this.FullScan));
                string p1 = directory;
                bool p2 = full;
                int p3 = sleep;

                object[] parameters = new object[] { p1, p2, p3 };

                _scanThread.Start(parameters);
            }
        }

        public void StartWatch(string directory)
        {
            _log.LogInfo("Starting watch in " + directory);
            //rewrite old watch
            _watcher = new FileSystemWatcher();
            _watcher.IncludeSubdirectories = true;
            _watcher.Path = Properties.Settings.Default.stngScanPath;
            _watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;

            //add other watchers?
            _watcher.Created += new FileSystemEventHandler(OnChanged);
            _watcher.Renamed += new RenamedEventHandler(OnChanged);
            _watcher.Changed += new FileSystemEventHandler(OnChanged);

            _watcher.EnableRaisingEvents = true;
        }
    }
}
