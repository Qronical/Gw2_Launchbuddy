﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Gw2_Launchbuddy.ObjectManagers
{
    public static class AccountManager
    {
        private static ObservableCollection<Account> accountCollection { get; set; }
        public static ReadOnlyObservableCollection<Account> AccountCollection { get; private set; }

        public static Account DefaultAccount { get; private set; }

        public static ObservableCollection<Account> SelectedAccountCollection { get => new ObservableCollection<Account>(accountCollection.Where(a => a.Selected==true)); }

        static AccountManager()
        {
            accountCollection = new ObservableCollection<Account>();
            AccountCollection = new ReadOnlyObservableCollection<Account>(accountCollection);

            DefaultAccount = new ObjectManagers.Account(null, null, null);
            AccountArgumentManager.StopGap.IsSelected("-shareArchive", true);
        }

        public static Account Account(string Nickname) => accountCollection.Where(a => a.Nickname == Nickname).Single();

        public static Account Add(string Nickname, string Email, string Password) => Add(new Account(Nickname, Email, Password));

        public static Account Add(Account Account)
        {
            accountCollection.Add(Account);
            foreach (Argument Argument in ArgumentManager.ArgumentCollection)
            {
                AccountArgumentManager.Add(Account, Argument);
            }
            foreach (AccountArgument AccountArgument in AccountArgumentManager.GetAccountArguments(DefaultAccount))
            {
                var temp = AccountArgumentManager.Get(Account, AccountArgument.Argument.Flag).IsSelected(AccountArgument.Selected);
                if (AccountArgument.Argument.Flag != "-email" && AccountArgument.Argument.Flag != "-password") temp.OptionString = AccountArgument.OptionString;
            }
            return Account;
        }

        public static void Remove(this Account Account) => accountCollection.Remove(Account);

        public static void Move(Account Account, int Incriment)
        {
            var index = accountCollection.IndexOf(Account);
            accountCollection.RemoveAt(index);
            index = (1 <= index ? index : 1);
            if (index < accountCollection.Count() || Incriment <= 0)
                accountCollection.Insert(index + Incriment, Account);
            else accountCollection.Add(Account);
        }

        public static void SetSelected(IEnumerable<Account> accounts)
        {
            foreach (var account in accountCollection)
            {
                account.Selected = false;
            }

            foreach (var account in accounts)
            {
                account.Selected = true;
            }             
        }

        public static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static class ImportExport
        {
            public static void LoadAccountInfo()
            {
                try
                {
                    var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Guild Wars 2\Launchbuddy.bin";

                    if (File.Exists(path) == true)
                    {
                        using (Stream stream = File.Open(path, FileMode.Open))
                        {
                            var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                            ObservableCollection<Account> aes_accountlist = (ObservableCollection<Account>)bformatter.Deserialize(stream);

                            foreach (Account acc in aes_accountlist)
                            {
                                Add(acc);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }

            public static void SaveAccountInfo()
            {
                ObservableCollection<Account> aes_accountlist = new ObservableCollection<Account>();
                try
                {
                    aes_accountlist.Clear();
                    foreach (Account acc in AccountManager.AccountCollection)
                    {
                        aes_accountlist.Add(acc);
                    }
                }
                catch (Exception err)
                {
                    MessageBox.Show("Could not encrypt passwords\n" + err.Message);
                }

                try
                {
                    var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Guild Wars 2\Launchbuddy.bin";
                    using (Stream stream = System.IO.File.Open(path, FileMode.Create))
                    {
                        var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                        bformatter.Serialize(stream, aes_accountlist);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
        }
    }

    [Serializable()]
    public class Account : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private string email;
        private string password;

        public string Nickname { get; set; }

        public string Email
        {
            get => email;
            set
            {
                email = value;
                AccountArgumentManager.Get(this, "-email")?.WithOptionString(value);
            }
        }

        public string ObscuredEmail
        {
            get
            {
                return email.Split('@')[0].Substring(0,2) + "***" + email.Split('@')[1];
            }
        }

        public string Password
        {
            get => password;
            set
            {
                password = value;
                AccountArgumentManager.Get(this, "-password")?.WithOptionString(value);
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public DateTime CreateDate { get; set; }
        public DateTime ModifyDate { get; set; }
        public DateTime RunDate { get; set; }

        public Account IsSelected(bool Selected = true)
        {
            this.Selected = Selected; return this;
        }

        [NonSerialized]
        public bool selected;

        public bool Selected { get => selected; set { selected = value; NotifyPropertyChanged(); } }

        public Account(string Nickname, string Email, string Password)
        {
            this.Nickname = Nickname;
            this.Email = Email;
            this.Password = Password;
            this.CreateDate = DateTime.Now;
            this.ModifyDate = DateTime.Now;
        }

        private string configurationPath;

        public string ConfigurationPath
        {
            get => String.IsNullOrWhiteSpace(configurationPath) ? "Default" : configurationPath;
            set => configurationPath = value;
        }

        public string ConfigurationName
        {
            get => Path.GetFileNameWithoutExtension(ConfigurationPath);
        }

        [NonSerialized]
        private static ImageSource defaultIcon;

        public static ImageSource DefaultIcon
        {
            get
            {
                if (defaultIcon == null)
                {
                    using (MemoryStream memory = new MemoryStream())
                    {
                        System.Drawing.Bitmap bitmap = Gw2_Launchbuddy.Properties.Resources.user;
                        bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                        memory.Position = 0;
                        BitmapImage bitmapimage = new BitmapImage();
                        bitmapimage.BeginInit();
                        bitmapimage.StreamSource = memory;
                        bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapimage.EndInit();

                        defaultIcon = bitmapimage;
                    }
                }
                return defaultIcon;
            }
        }

        [NonSerialized]
        private ImageSource icon;

        public ImageSource Icon
        {
            get => icon ?? DefaultIcon;
            private set => icon = value;
        }

        public void SetIcon(string Path)
        {
            if (System.IO.File.Exists(Path))
            {
                var bitmap = new BitmapImage();
                using (var stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();

                    icon = bitmap;
                }
            }
        }

        public AccountArgument Argument(string Flag) => AccountArgumentManager.GetOrCreate(this, Flag);

        public List<AccountArgument> GetArgumentList() => AccountArgumentManager.GetAccountArguments(this);

        public string PrintArguments() => String.Join(" ", GetArgumentList().Where(a => a.Selected == true).Select(a => a.Argument.Flag + (a.Argument.Sensitive ? null : " " + a.OptionString)));

        public string CommandLine() => String.Join(" ", GetArgumentList().Where(a => a.Selected == true).Select(a => a.Argument.Flag + (!String.IsNullOrWhiteSpace(a.OptionString) ? " " + a.OptionString : null)));

        public void Move(int Incriment) => AccountManager.Move(this, Incriment);

        public Client CreateClient()
        {
            var Client = ClientManager.CreateClient();
            AccountClientManager.Add(this, Client); //Not sure this is the best place for this create/assign
            return Client;
        }
    }
}