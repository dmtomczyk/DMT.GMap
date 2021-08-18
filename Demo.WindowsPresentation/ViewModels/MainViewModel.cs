using System;
using System.Collections.ObjectModel;
using Demo.WindowsPresentation.CustomMarkers;
using GalaSoft.MvvmLight;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;

namespace Demo.WindowsPresentation.ViewModels
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            // Tampa, FL.
            PointLatLng tampa = new PointLatLng(28.034, -82.640);

            MapPosition = tampa;
            GMapMarker tpaMarker = new GMapMarker(tampa);
            {
                tpaMarker.Shape = new CustomMarkerRed(null, tpaMarker, "Custom marker");
                tpaMarker.Offset = new System.Windows.Point(-15, -15);
                tpaMarker.ZIndex = int.MaxValue;
            }

            AllMarkers.Add(tpaMarker);
        }

        #region Properties

        private bool _canDragMap = true;
        public bool CanDragMap
        {
            get => _canDragMap;
            set => Set(ref _canDragMap, value);
        }

        private PointLatLng _mapPosition;
        public PointLatLng MapPosition
        {
            get => _mapPosition;
            set => Set(ref _mapPosition, value);
        }

        private ObservableCollection<GMapMarker> _allMarkers = new ObservableCollection<GMapMarker>();
        public ObservableCollection<GMapMarker> AllMarkers
        {
            get => _allMarkers;
            set => Set(ref _allMarkers, value);
        }

        private GMapMarker _selectedLocation;
        public GMapMarker SelectedLocation
        {
            get => _selectedLocation;
            set => Set(ref _selectedLocation, value);
        }

        private bool _isCheckBoxPlaceChecked = false;
        public bool IsCheckBoxPlaceChecked
        {
            get => _isCheckBoxPlaceChecked;
            set => Set(ref _isCheckBoxPlaceChecked, value);
        }

        private bool _isGridVisible;
        public bool IsGridVisible
        {
            get => _isGridVisible;
            set => Set(ref _isGridVisible, value);
        }

        #endregion

        #region Commands



        #endregion

        internal void AddMarker(MainWindow mainWindow)
        {
            GMapMarker m = new GMapMarker(SelectedLocation.Position);
            {
                Placemark? p = null;

                if (IsCheckBoxPlaceChecked)
                {
                    Placemark? plret = GMapProviders.GoogleMap.GetPlacemark(SelectedLocation.Position, out GeoCoderStatusCode status);

                    if (status == GeoCoderStatusCode.G_GEO_SUCCESS && plret != null)
                    {
                        p = plret;
                    }
                }

                string toolTipText = (p != null)
                    ? p.Value.Address
                    : SelectedLocation.Position.ToString();

                m.Shape = new CustomMarkerDemo(mainWindow, m, toolTipText);
                m.ZIndex = 55;
            }

            AllMarkers.Add(m);
        }

    }
}