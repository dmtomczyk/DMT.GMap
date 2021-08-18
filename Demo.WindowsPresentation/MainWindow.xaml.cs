using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Demo.WindowsForms;
using Demo.WindowsPresentation.CustomMarkers;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using System.Net;
using Demo.WindowsPresentation.ViewModels;
using CommonServiceLocator;

namespace Demo.WindowsPresentation
{
    public partial class MainWindow : Window
    {

        PointLatLng _start;
        PointLatLng _end;

        // marker
        GMapMarker currentMarker;

        // zones list
        List<GMapMarker> Circles = new List<GMapMarker>();

        public MainWindow()
        {
            InitializeComponent();

            // TODO: set cache mode only if no internet avaible
            //if (!Stuff.PingNetwork("pingtest.com"))
            //{
            //    MainMap.Manager.Mode = AccessMode.CacheOnly;
            //    MessageBox.Show("No internet connection available, going to CacheOnly mode.",
            //        "GMap.NET - Demo.WindowsPresentation",
            //        MessageBoxButton.OK,
            //        MessageBoxImage.Warning);
            //}

            //GoogleMapProvider.Instance.ApiKey = Stuff.GoogleMapsApiKey;
            GoogleMapProvider.Instance.ApiKey = "AIzaSyByDLmGnyM5-n9aWntdPXkCP8qHD0i5A50";

            // config map
            MainMap.MapProvider = GMapProviders.OpenStreetMap;
            MainMap.Position = new PointLatLng(54.6961334816182, 25.2985095977783);

            //// 20200313 (jokubokla): Demo of the new Sweden Map with Mercator instead of SWEREF99
            //MainMap.MapProvider = GMapProviders.SwedenMapAlternative;
            //MainMap.Position = new PointLatLng(58.406298501604, 15.5825614929199); // Linköping
            //MainMap.MinZoom = 1;
            //MainMap.MaxZoom = 15;
            //MainMap.Zoom = 11;
            //TextBoxGeo.Text = "Linköping";

            //MainMap.ScaleMode = ScaleModes.Dynamic;

            // get map types
            ComboBoxMapType.ItemsSource = GMapProviders.List;
            ComboBoxMapType.DisplayMemberPath = "Name";
            ComboBoxMapType.SelectedItem = MainMap.MapProvider;

            // acccess mode
            ComboBoxMode.ItemsSource = Enum.GetValues(typeof(AccessMode));
            ComboBoxMode.SelectedItem = MainMap.Manager.Mode;

            // get cache modes
            CheckBoxCacheRoute.IsChecked = MainMap.Manager.UseRouteCache;
            CheckBoxGeoCache.IsChecked = MainMap.Manager.UseGeocoderCache;

            // setup zoom min/max
            SliderZoom.Maximum = MainMap.MaxZoom;
            SliderZoom.Minimum = MainMap.MinZoom;

            // get position
            TextBoxLat.Text = MainMap.Position.Lat.ToString(CultureInfo.InvariantCulture);
            TextBoxLng.Text = MainMap.Position.Lng.ToString(CultureInfo.InvariantCulture);

            // get marker state
            CheckBoxCurrentMarker.IsChecked = true;

            // can drag map
            CheckBoxDragMap.IsChecked = MainMap.CanDragMap;

#if DEBUG
            CheckBoxDebug.IsChecked = true;
#endif

            //validator.Window = this;

            // set current marker
            currentMarker = new GMapMarker(MainMap.Position);
            {
                currentMarker.Shape = new CustomMarkerRed(this.MainMap, currentMarker, "custom position marker");
                currentMarker.Offset = new Point(-15, -15);
                currentMarker.ZIndex = int.MaxValue;
                MainMap.Markers.Add(currentMarker);
            }

            //if(false)
            {
                // add my city location for demo
                GeoCoderStatusCode status;

                var city = GMapProviders.GoogleMap.GetPoint(this.TextBoxGeo.Text ?? "Tampa, FL", out status);
                if (city != null && status == GeoCoderStatusCode.G_GEO_SUCCESS)
                {
                    var it = new GMapMarker(city.Value);
                    {
                        it.ZIndex = 55;
                        it.Shape = new CustomMarkerDemo(this, it, "Welcome to Lithuania! ;}");
                    }
                    MainMap.Markers.Add(it);

                    #region -- add some markers and zone around them --

                    //if(false)
                    {
                        var objects = new List<PointAndInfo>();
                        {
                            string area = "Antakalnis";
                            var pos = GMapProviders.GoogleMap.GetPoint("Lithuania, Vilnius, " + area, out status);
                            if (pos != null && status == GeoCoderStatusCode.G_GEO_SUCCESS)
                            {
                                objects.Add(new PointAndInfo(pos.Value, area));
                            }
                        }
                        {
                            string area = "Senamiestis";
                            var pos = GMapProviders.GoogleMap.GetPoint("Lithuania, Vilnius, " + area, out status);
                            if (pos != null && status == GeoCoderStatusCode.G_GEO_SUCCESS)
                            {
                                objects.Add(new PointAndInfo(pos.Value, area));
                            }
                        }
                        {
                            string area = "Pilaite";
                            var pos = GMapProviders.GoogleMap.GetPoint("Lithuania, Vilnius, " + area, out status);
                            if (pos != null && status == GeoCoderStatusCode.G_GEO_SUCCESS)
                            {
                                objects.Add(new PointAndInfo(pos.Value, area));
                            }
                        }
                        AddDemoZone(8.8, city.Value, objects);
                    }

                    #endregion
                }

                if (MainMap.Markers.Count > 1)
                {
                    MainMap.ZoomAndCenterMarkers(null);
                }
            }
        }

        private void MainMap_MouseEnter(object sender, MouseEventArgs e)
        {
            MainMap.Focus();
        }

        // add objects and zone around them
        private void AddDemoZone(double areaRadius, PointLatLng center, List<PointAndInfo> objects)
        {
            var objectsInArea = from p in objects
                                where MainMap.MapProvider.Projection.GetDistance(center, p.Point) <= areaRadius
                                select new { Obj = p, Dist = MainMap.MapProvider.Projection.GetDistance(center, p.Point) };
            if (objectsInArea.Any())
            {
                var maxDistObject = (from p in objectsInArea
                                     orderby p.Dist descending
                                     select p).First();

                // add objects to zone
                foreach (var o in objectsInArea)
                {
                    var it = new GMapMarker(o.Obj.Point);
                    {
                        it.ZIndex = 55;
                        var s = new CustomMarkerDemo(this,
                            it,
                            o.Obj.Info + ", distance from center: " + o.Dist + "km.");
                        it.Shape = s;
                    }

                    MainMap.Markers.Add(it);
                }

                // add zone circle
                //if(false)
                {
                    var it = new GMapMarker(center);
                    it.ZIndex = -1;

                    var c = new Circle();
                    c.Center = center;
                    c.Bound = maxDistObject.Obj.Point;
                    c.Tag = it;
                    c.IsHitTestVisible = false;

                    UpdateCircle(c);
                    Circles.Add(it);

                    it.Shape = c;
                    MainMap.Markers.Add(it);
                }
            }
        }

        // calculates circle radius
        private void UpdateCircle(Circle c)
        {
            var pxCenter = MainMap.FromLatLngToLocal(c.Center);
            var pxBounds = MainMap.FromLatLngToLocal(c.Bound);

            double a = pxBounds.X - pxCenter.X;
            double b = pxBounds.Y - pxCenter.Y;
            double pxCircleRadius = Math.Sqrt(a * a + b * b);

            c.Width = 55 + pxCircleRadius * 2;
            c.Height = 55 + pxCircleRadius * 2;
            (c.Tag as GMapMarker).Offset = new Point(-c.Width / 2, -c.Height / 2);
        }

        private void MainMap_OnMapTypeChanged(GMapProvider type)
        {
            SliderZoom.Minimum = MainMap.MinZoom;
            SliderZoom.Maximum = MainMap.MaxZoom;
        }

        private void MainMap_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.GetPosition(MainMap) is Point currentPosition
                && DataContext is MainViewModel mvm)
            {
                PointLatLng position = MainMap.FromLocalToLatLng((int)currentPosition.X, (int)currentPosition.Y);
                GMapMarker newMarker = new GMapMarker(position);
                mvm.SelectedLocation = newMarker;
            }
        }

        // move current marker with left holding
        private void MainMap_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var p = e.GetPosition(MainMap);
                currentMarker.Position = MainMap.FromLocalToLatLng((int)p.X, (int)p.Y);
            }
        }

        // zoo max & center markers
        private void button13_Click(object sender, RoutedEventArgs e)
        {
            MainMap.ZoomAndCenterMarkers(null);

            /*
            PointAnimation panMap = new PointAnimation();
            panMap.Duration = TimeSpan.FromSeconds(1);
            panMap.From = new Point(MainMap.Position.Lat, MainMap.Position.Lng);
            panMap.To = new Point(0, 0);
            Storyboard.SetTarget(panMap, MainMap);
            Storyboard.SetTargetProperty(panMap, new PropertyPath(GMapControl.MapPointProperty));
   
            Storyboard panMapStoryBoard = new Storyboard();
            panMapStoryBoard.Children.Add(panMap);
            panMapStoryBoard.Begin(this);
             */
        }

        // tile louading starts
        private void MainMap_OnTileLoadStart()
        {
            try
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                    new Action(() =>
                    {
                        ProgressBar1.Visibility = Visibility.Visible;
                    }));
            }
            catch
            {
            }
        }

        // tile loading stops
        private void MainMap_OnTileLoadComplete(long elapsedMilliseconds)
        {
            MainMap.ElapsedMilliseconds = elapsedMilliseconds;

            try
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                    new Action(() =>
                    {
                        ProgressBar1.Visibility = Visibility.Hidden;
                        GroupBox3.Header = "loading, last in " + MainMap.ElapsedMilliseconds + "ms";
                    }));
            }
            catch
            {
            }
        }

        // current location changed
        private void MainMap_OnCurrentPositionChanged(PointLatLng point)
        {
            MapGroup.Header = "gmap: " + point;
        }

        // reload
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            MainMap.ReloadMap();
        }

        // enable current marker
        private void checkBoxCurrentMarker_Checked(object sender, RoutedEventArgs e)
        {
            if (currentMarker != null)
            {
                MainMap.Markers.Add(currentMarker);
            }
        }

        // disable current marker
        private void checkBoxCurrentMarker_Unchecked(object sender, RoutedEventArgs e)
        {
            if (currentMarker != null)
            {
                MainMap.Markers.Remove(currentMarker);
            }
        }

        // enable map dragging
        private void checkBoxDragMap_Checked(object sender, RoutedEventArgs e)
        {
            MainMap.CanDragMap = true;
        }

        // disable map dragging
        private void checkBoxDragMap_Unchecked(object sender, RoutedEventArgs e)
        {
            MainMap.CanDragMap = false;
        }

        private void Navigate_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel bigMapVM)
            {
                if (double.TryParse(TextBoxLat.Text, out double latitude)
                    && double.TryParse(TextBoxLng.Text, out double longitude))
                {
                    MainMap.Position = new PointLatLng(latitude, longitude);
                }
            }
        }

        /// <summary>
        /// Enter key pressed while the GeoTextBox is active.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GeoTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (DataContext is MainViewModel bigMapVM)
            {
                if (e.Key == Key.Enter)
                {
                    GeoCoderStatusCode status = MainMap.SetPositionByKeywords(TextBoxGeo.Text);

                    if (status != GeoCoderStatusCode.G_GEO_SUCCESS)
                    {
                        MessageBox.Show(
                            messageBoxText: $"Geocoder can't find: {TextBoxGeo.Text}, reason: {status}",
                            caption: "GMap.NET",
                            MessageBoxButton.OK,
                            MessageBoxImage.Exclamation);
                    }
                    else currentMarker.Position = MainMap.Position;
                }
            }
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // updates circles on map
            foreach (GMapMarker marker in Circles)
            {
                UpdateCircle(marker.Shape as Circle);
            }
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            MainMap.Zoom = (int)MainMap.Zoom + 1;
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            MainMap.Zoom = (int)(MainMap.Zoom + 0.99) - 1;
        }

        /// <summary>
        /// Predownload / fetch tiles at varying zooms.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Prefetch_Click(object sender, RoutedEventArgs e)
        {
            var area = MainMap.SelectedArea;
            if (!area.IsEmpty)
            {
                for (int i = (int)MainMap.Zoom; i <= MainMap.MaxZoom; i++)
                {
                    MessageBoxResult res = MessageBox.Show("Ready ripp at Zoom = " + i + " ?",
                        "GMap.NET",
                        MessageBoxButton.YesNoCancel);

                    if (res == MessageBoxResult.Yes)
                    {
                        var obj = new TilePrefetcher
                        {
                            // TODO: THIS!!!!
                            //obj.Owner = this;
                            ShowCompleteMessage = true
                        };
                        obj.Start(area, i, MainMap.MapProvider, 100);
                    }
                    else if (res == MessageBoxResult.No)
                    {
                        continue;
                    }
                    else if (res == MessageBoxResult.Cancel)
                    {
                        break;
                    }
                }
            }
            else
            {
                MessageBox.Show("Select map area holding ALT",
                    "GMap.NET",
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
            }
        }

        // access mode
        private void comboBoxMode_DropDownClosed(object sender, EventArgs e)
        {
            MainMap.Manager.Mode = (AccessMode)ComboBoxMode.SelectedItem;
            MainMap.ReloadMap();
        }

        // clear cache
        private void button4_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are You sure?",
                    "Clear GMap.NET cache?",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                try
                {
                    MainMap.Manager.PrimaryCache.DeleteOlderThan(DateTime.Now, null);
                    MessageBox.Show("Done. Cache is clear.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        // export
        private void button6_Click(object sender, RoutedEventArgs e)
        {
            MainMap.ShowExportDialog();
        }

        // import
        private void button5_Click(object sender, RoutedEventArgs e)
        {
            MainMap.ShowImportDialog();
        }

        // use route cache
        private void checkBoxCacheRoute_Checked(object sender, RoutedEventArgs e)
        {
            MainMap.Manager.UseRouteCache = CheckBoxCacheRoute.IsChecked.Value;
        }

        // use geocoding cahce
        private void checkBoxGeoCache_Checked(object sender, RoutedEventArgs e)
        {
            MainMap.Manager.UseGeocoderCache = CheckBoxGeoCache.IsChecked.Value;
            MainMap.Manager.UsePlacemarkCache = MainMap.Manager.UseGeocoderCache;
        }

        // save currnt view
        private void button7_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var img = MainMap.ToImageSource();
                var en = new PngBitmapEncoder();
                en.Frames.Add(BitmapFrame.Create(img as BitmapSource));

                var dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = "GMap.NET Image"; // Default file name
                dlg.DefaultExt = ".png"; // Default file extension
                dlg.Filter = "Image (.png)|*.png"; // Filter files by extension
                dlg.AddExtension = true;
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                // Show save file dialog box
                var result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result == true)
                {
                    // Save document
                    string filename = dlg.FileName;

                    using (Stream st = File.OpenWrite(filename))
                    {
                        en.Save(st);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // clear all markers
        private void button10_Click(object sender, RoutedEventArgs e)
        {
            var clear = MainMap.Markers.Where(p => p != null && p != currentMarker);
            if (clear != null)
            {
                for (int i = 0; i < clear.Count(); i++)
                {
                    MainMap.Markers.Remove(clear.ElementAt(i));
                    i--;
                }
            }
        }

        // add marker
        private void button8_Click(object sender, RoutedEventArgs e)
        {

            if (DataContext is MainViewModel bigMapVM)
            {
                bigMapVM.AddMarker(this);
            }

            //var m = new GMapMarker(currentMarker.Position);
            //{
            //    Placemark? p = null;
            //    if (CheckBoxPlace.IsChecked.Value)
            //    {
            //        GeoCoderStatusCode status;
            //        var plret = GMapProviders.GoogleMap.GetPlacemark(currentMarker.Position, out status);
            //        if (status == GeoCoderStatusCode.G_GEO_SUCCESS && plret != null)
            //        {
            //            p = plret;
            //        }
            //    }

            //    string toolTipText;
            //    if (p != null)
            //    {
            //        toolTipText = p.Value.Address;
            //    }
            //    else
            //    {
            //        toolTipText = currentMarker.Position.ToString();
            //    }

            //    m.Shape = new CustomMarkerDemo(this, m, toolTipText);
            //    m.ZIndex = 55;
            //}
            //MainMap.Markers.Add(m);
        }

        // sets route start
        private void button11_Click(object sender, RoutedEventArgs e)
        {
            _start = currentMarker.Position;
        }

        // sets route end
        private void button9_Click(object sender, RoutedEventArgs e)
        {
            _end = currentMarker.Position;
        }

        // adds route
        private void button12_Click(object sender, RoutedEventArgs e)
        {
            var rp = MainMap.MapProvider as RoutingProvider;
            if (rp == null)
            {
                rp = GMapProviders.OpenStreetMap; // use OpenStreetMap if provider does not implement routing
            }

            var route = rp.GetRoute(_start, _end, false, false, (int)MainMap.Zoom);
            if (route != null)
            {
                var m1 = new GMapMarker(_start);
                m1.Shape = new CustomMarkerDemo(this, m1, "Start: " + route.Name);

                var m2 = new GMapMarker(_end);
                m2.Shape = new CustomMarkerDemo(this, m2, "End: " + _start.ToString());

                var mRoute = new GMapRoute(route.Points);
                {
                    mRoute.ZIndex = -1;
                }

                MainMap.Markers.Add(m1);
                MainMap.Markers.Add(m2);
                MainMap.Markers.Add(mRoute);

                MainMap.ZoomAndCenterMarkers(null);
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            int offset = 22;

            if (MainMap.IsFocused)
            {
                if (e.Key == Key.Left)
                {
                    MainMap.Offset(-offset, 0);
                }
                else if (e.Key == Key.Right)
                {
                    MainMap.Offset(offset, 0);
                }
                else if (e.Key == Key.Up)
                {
                    MainMap.Offset(0, -offset);
                }
                else if (e.Key == Key.Down)
                {
                    MainMap.Offset(0, offset);
                }
                else if (e.Key == Key.Add)
                {
                    ZoomIn_Click(null, null);
                }
                else if (e.Key == Key.Subtract)
                {
                    ZoomOut_Click(null, null);
                }
            }
        }

        // set real time demo
        private void RealTimeChanged(object sender, RoutedEventArgs e)
        {
            MainMap.Markers.Clear();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                MainMap.Bearing--;
            }
            else if (e.Key == Key.Z)
            {
                MainMap.Bearing++;
            }
        }
    }
}