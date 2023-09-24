using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace RailwaymapUI
{
    public class FileHistoryItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string Name { get; private set; }
        public string FullPath { get; private set; }

        public FileHistoryItem(string name, string fullPath)
        {
            Name = name;
            FullPath = fullPath;
        }
    }

    public class FileHistory : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private const int HISTORY_MAX_ITEMS = 6;

        private const string HISTORY_KEY = "FileHistory";

        private readonly string set_filename;

        public List<FileHistoryItem> Items { get; private set; }

        public FileHistory(string settings_filename)
        {
            char[] delim = new char[1] { '=' };

            set_filename = settings_filename;

            Items = new List<FileHistoryItem>();

            if (File.Exists(settings_filename))
            {
                string[] lines = File.ReadAllLines(settings_filename);

                foreach (string line in lines)
                {
                    string[] parts = line.Split(delim, 2);

                    if (parts.Length == 2)
                    {
                        if (parts[0] == HISTORY_KEY)
                        {
                            if (Items.Count < HISTORY_MAX_ITEMS)
                            {
                                Items.Add(new FileHistoryItem(Path.GetFileName(parts[1]), parts[1]));
                            }
                        }
                    }
                }
            }
        }

        public void Add_Item(string fullpath)
        {
            // Insert new item to first position
            Items.Insert(0, new FileHistoryItem(Path.GetFileName(fullpath), fullpath));

            // If same item is already elsewhere in the list, remove it
            for (int i = Items.Count - 1; i >= 1; i--)
            {
                if (Items[i].FullPath == fullpath)
                {
                    Items.RemoveAt(i);
                }
            }

            if (Items.Count > HISTORY_MAX_ITEMS)
            {
                Items.RemoveRange(HISTORY_MAX_ITEMS, Items.Count - HISTORY_MAX_ITEMS);
            }

            List<string> new_settings = new List<string>();

            foreach (FileHistoryItem item in Items)
            {
                new_settings.Add(HISTORY_KEY + "=" + item.FullPath);
            }

            if (File.Exists(set_filename))
            {
                char[] delim = new char[1] { '=' };

                string[] lines = File.ReadAllLines(set_filename);

                foreach (string line in lines)
                {
                    string[] parts = line.Split(delim, 2);

                    if (parts.Length == 2)
                    {
                        if (parts[0] != HISTORY_KEY)
                        {
                            new_settings.Add(line);
                        }
                    }
                }
            }

            File.WriteAllLines(set_filename, new_settings.ToArray());

            OnPropertyChanged(nameof(Items));
        }
    }
}
