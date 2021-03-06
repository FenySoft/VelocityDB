﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VelocityDBExtensions;
using DatabaseManager.Model;
using VelocityDb.Session;
using System.Net;
using VelocityDb;
using VelocityDb.Collection;
using System.IO;

namespace DatabaseManager
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    AllFederationsViewModel m_viewModel;
    string _dbFilePath;
    public MainWindow(string dbFilePath)
    {
      InitializeComponent();
      //SessionBase.s_serverTcpIpPortNumber = 7032;
      _dbFilePath = dbFilePath;
      m_viewModel = new AllFederationsViewModel();
      DirectoryInfo dirInfo = m_viewModel.Initialize(dbFilePath);
      //DataCache.MaximumMemoryUse = 3000000000; // 3 GB, set this to what fits your case
      bool addedFd = false;
      if (dirInfo != null)
        addedFd = AddFederation(dirInfo);
      if (addedFd == false)
        base.DataContext = m_viewModel;
    }

    bool AddFederation(DirectoryInfo dirInfo)
    {
      FederationInfo info = new FederationInfo();
      if (dirInfo != null)
        info.SystemDbsPath = dirInfo.FullName;
      ConnectionDialog popup = new ConnectionDialog(info);
      bool? result = popup.ShowDialog();
      if (result != null && result.Value)
      {
        if (info.HostName == null || info.HostName.Length == 0)
          info.HostName = SessionBase.LocalHost;
        SessionBase session = m_viewModel.ActiveSession;
        if (session.InTransaction)
          session.Commit();
        session.BeginUpdate();
        session.Persist(info);
        session.Commit();
        m_viewModel = new AllFederationsViewModel();
        base.DataContext = m_viewModel;
        return true;
      }
      return false;
    }
    private void AddMenuItem_Click(object sender, RoutedEventArgs e)
    {
      AddFederation(null);
    }

    private void SchemaMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var cw = new Schema(_dbFilePath);
      cw.ShowInTaskbar = false;
      cw.Owner = this;
      cw.Show();
    }

    private void Create1000TestObjectsMenuItem_Click(object sender, RoutedEventArgs e)
    {
      MenuItem menuItem = (MenuItem)sender;
      FederationViewModel view = (FederationViewModel)menuItem.DataContext;
      FederationInfo info = view.Federationinfo;
      SessionBase session = view.Session;
      if (session.InTransaction)
        session.Commit();
      session.BeginUpdate();
      //session.EnableSyncByTrackingChanges = true;
      try
      {
        for (int i = 0; i < 1000; i++)
        {
          VelocityDbList<OptimizedPersistable> list = new VelocityDbList<OptimizedPersistable>();
          //for (int j = 0; j < 10; j++)
          //  list.Add(new OptimizedPersistable());
          session.Persist(list);
        }
        session.Commit();
        m_viewModel = new AllFederationsViewModel();
        base.DataContext = m_viewModel;
      }
      catch (Exception ex)
      {
        session.Abort();
        MessageBox.Show(ex.Message);
      }
    }

    private void DatabaseLocationOrderByMenuItem_Click(object sender, RoutedEventArgs e)
    {
      MenuItem menuItem = (MenuItem)sender;
      DatabaseLocationViewModel view = (DatabaseLocationViewModel)menuItem.DataContext;
      view.OrderDatabasesByName = menuItem.IsChecked;
    }

    private void VelocityGraphModeMenuItem_Click(object sender, RoutedEventArgs e)
    {
      MenuItem menuItem = (MenuItem)sender;
      var view = (FederationViewModel)menuItem.DataContext;
      view.VelocityGraphMode = menuItem.IsChecked;
    }

    private void RemoveDatabaseLocationMenuItem_Click(object sender, RoutedEventArgs e)
    {
      MenuItem menuItem = (MenuItem)sender;
      DatabaseLocationViewModel view = (DatabaseLocationViewModel)menuItem.DataContext;
      DatabaseLocation dbLocation = view.DatabaseLocation;
      SessionBase session = dbLocation.GetSession();
      if (session.InTransaction)
        session.Commit();
      session.BeginUpdate();
      try
      {
        session.DeleteLocation(dbLocation);
        session.Commit();
        m_viewModel = new AllFederationsViewModel();
        base.DataContext = m_viewModel;
      }
      catch (Exception ex)
      {
        session.Abort();
        MessageBox.Show(ex.Message);
      }
    }

    private void RestoreDatabaseLocationMenuItem_Click(object sender, RoutedEventArgs e)
    {
      MenuItem menuItem = (MenuItem)sender;
      DatabaseLocationViewModel view = (DatabaseLocationViewModel)menuItem.DataContext;
      DatabaseLocation dbLocation = view.DatabaseLocation;
      SessionBase session = dbLocation.GetSession();
      if (session.InTransaction)
        session.Commit();
      //DatabaseLocationMutable newLocationMutable = new DatabaseLocationMutable(session);
      //newLocationMutable.DirectoryPath = dbLocation.DirectoryPath;
      //newLocationMutable.HostName = dbLocation.HostName;
      //var popup = new RestoreDialog(newLocationMutable);
      //bool? result = popup.ShowDialog();
      //if (result != null && result.Value)
      {
        dbLocation.SetPage(null); // fake it as a transient object before restore !
        dbLocation.Id = 0;      // be careful about doing this kind of make transient tricks, references from objects like this are still persistent.
       // if (session.OptimisticLocking) // && session.GetType() == typeof(ServerClientSession))
        {
         // session.Dispose();
         // session = new ServerClientSession(session.SystemDirectory, session.SystemHostName, 2000, false, false); // need to use pessimstic locking for restore
          // = new SessionNoServer(session.SystemDirectory); // need to use pessimstic locking for restore
        }
        session.BeginUpdate();
        try
        {
          session.RestoreFrom(dbLocation, DateTime.Now);
          session.Commit(false, true); // special flags when commit of a restore ...
          m_viewModel = new AllFederationsViewModel();
          base.DataContext = m_viewModel;
        }
        catch (Exception ex)
        {
          session.Abort();
          MessageBox.Show(ex.Message);
        }
      }
    }

    private void EditDatabaseLocationMenuItem_Click(object sender, RoutedEventArgs e)
    {
      MenuItem menuItem = (MenuItem)sender;
      DatabaseLocationViewModel view = (DatabaseLocationViewModel)menuItem.DataContext;
      DatabaseLocation dbLocation = view.DatabaseLocation;
      SessionBase session = dbLocation.GetSession();
      DatabaseLocationMutable newLocationMutable = new DatabaseLocationMutable(session);
      newLocationMutable.BackupOfOrForLocation = dbLocation.BackupOfOrForLocation;
      newLocationMutable.CompressPages = dbLocation.CompressPages;
      newLocationMutable.PageEncryption = dbLocation.PageEncryption;
      newLocationMutable.StartDatabaseNumber = dbLocation.StartDatabaseNumber;
      newLocationMutable.EndDatabaseNumber = dbLocation.EndDatabaseNumber;
      newLocationMutable.IsBackupLocation = dbLocation.IsBackupLocation;
      newLocationMutable.DirectoryPath = dbLocation.DirectoryPath;
      newLocationMutable.HostName = dbLocation.HostName;
      if (dbLocation.DesKey != null)
        newLocationMutable.DesKey = SessionBase.TextEncoding.GetString(dbLocation.DesKey, 0, dbLocation.DesKey.Length);

      var popup = new NewDatabaseLocationDialog(newLocationMutable, dbLocation);
      bool? result = popup.ShowDialog();
      if (result != null && result.Value)
      {
        try
        {
          DatabaseLocation newLocation = new DatabaseLocation(newLocationMutable.HostName, newLocationMutable.DirectoryPath, newLocationMutable.StartDatabaseNumber,
            newLocationMutable.EndDatabaseNumber, session, newLocationMutable.CompressPages, newLocationMutable.PageEncryption, newLocationMutable.IsBackupLocation,
            newLocationMutable.IsBackupLocation ? newLocationMutable.BackupOfOrForLocation : dbLocation.BackupOfOrForLocation);
          if (session.InTransaction)
            session.Commit();
          session.BeginUpdate();
          newLocation = session.NewLocation(newLocation);
          newLocation.DesKey = SessionBase.TextEncoding.GetBytes(newLocationMutable.DesKey);
          session.Commit();
          m_viewModel = new AllFederationsViewModel();
          base.DataContext = m_viewModel;
        }
        catch (Exception ex)
        {
          session.Abort();
          MessageBox.Show(ex.Message);
        }
      }
    }

    void AddDatabaseLocation(SessionBase session, string directory)
    {
      DatabaseLocationMutable newLocationMutable = new DatabaseLocationMutable(session);
      newLocationMutable.DirectoryPath = directory;
      var popup = new NewDatabaseLocationDialog(newLocationMutable, null);
      bool? result = popup.ShowDialog();
      if (result != null && result.Value)
      {
        try
        {
          DatabaseLocation newLocation = new DatabaseLocation(newLocationMutable.HostName, newLocationMutable.DirectoryPath, newLocationMutable.StartDatabaseNumber,
            newLocationMutable.EndDatabaseNumber, session, newLocationMutable.CompressPages, newLocationMutable.PageEncryption, newLocationMutable.BackupOfOrForLocation != null,
            newLocationMutable.BackupOfOrForLocation);
          if (session.InTransaction)
            session.Commit();
          session.BeginUpdate();
          session.NewLocation(newLocation);
          session.Commit();
          m_viewModel = new AllFederationsViewModel();
          base.DataContext = m_viewModel;
        }
        catch (Exception ex)
        {
          session.Abort();
          MessageBox.Show(ex.Message);
        }
      }
    }

    private void NewDatabaseLocationMenuItem_Click(object sender, RoutedEventArgs e)
    {
      MenuItem menuItem = (MenuItem)sender;
      FederationViewModel view = (FederationViewModel)menuItem.DataContext;
      FederationInfo info = view.Federationinfo;
      SessionBase session = view.Session;
      AddDatabaseLocation(session, "");
    }

    private void CopyFederationMenuItem_Click(object sender, RoutedEventArgs e)
    {
      MenuItem menuItem = (MenuItem)sender;
      FederationViewModel view = (FederationViewModel)menuItem.DataContext;
      FederationInfo info = view.Federationinfo;
      SessionBase session = view.Session;
      var lDialog = new System.Windows.Forms.FolderBrowserDialog()
      {
        Description = "Choose Federation Copy Folder",
      };
      if (lDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        string copyDir = lDialog.SelectedPath;
        if (session.InTransaction)
          session.Commit(); // must not be in transaction while copying databases
        session.CopyAllDatabasesTo(copyDir);
        session = info.GetSession();
        session.BeginUpdate();
        FederationCopyInfo copyInfo = new FederationCopyInfo(Dns.GetHostName(), copyDir);
        session.Persist(copyInfo);
        info.Update();
        info.FederationCopies.Add(copyInfo);
        session.Commit();
        MessageBox.Show("Databases copied to " + copyDir + " at " + DateTime.Now);
      }
    }

    private void SyncFederationMenuItem_Click(object sender, RoutedEventArgs e)
    {
      MenuItem menuItem = (MenuItem)sender;
      FederationViewModel view = (FederationViewModel)menuItem.DataContext;
      FederationInfo info = view.Federationinfo;
      SessionBase session = view.Session;
      var lDialog = new System.Windows.Forms.FolderBrowserDialog()
      {
        Description = "Choose Federation Sync Destination Folder",
      };
      if (lDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        string destdir = lDialog.SelectedPath;
        if (session.InTransaction)
          session.Commit(); // must not be in transaction while copying databases
        using (SessionBase sessionDestination = new SessionNoServer(destdir))
        {
#if NET_CORE
          
#else
          sessionDestination.SyncWith(session);
#endif
        }
        m_viewModel = new AllFederationsViewModel();
        base.DataContext = m_viewModel;
        MessageBox.Show("Databases synced with " + destdir + " at " + DateTime.Now);
      }
    }

    private void ValidateFederationMenuItem_Click(object sender, RoutedEventArgs e)
    {
      MenuItem menuItem = (MenuItem)sender;
      FederationViewModel view = (FederationViewModel)menuItem.DataContext;
      FederationInfo info = view.Federationinfo;
      SessionBase session = view.Session;
      session.Verify();
      session = info.GetSession();
      session.BeginUpdate();
      info.Update();
      info.Validated.Add(DateTime.Now);
      session.Commit();
      MessageBox.Show("Databases validated without errors, " + DateTime.Now);
    }

    private void RemoveFederationInfoMenuItem_Click(object sender, RoutedEventArgs e)
    {
      MenuItem menuItem = (MenuItem)sender;
      FederationViewModel view = (FederationViewModel)menuItem.DataContext;
      FederationInfo info = view.Federationinfo;
      SessionBase session = info.GetSession();
      session.BeginUpdate();
      info.Unpersist(session);
      session.Commit();
      m_viewModel = new AllFederationsViewModel();
      base.DataContext = m_viewModel;
    }
  }
}
