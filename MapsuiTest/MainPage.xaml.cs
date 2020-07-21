﻿using System;
using System.ComponentModel;
using System.Net.Http;
using Mapsui;
using Mapsui.Fetcher;
using Mapsui.Layers;
using Mapsui.Projection;
using Mapsui.Widgets;
using Mapsui.Widgets.ScaleBar;
using Mapsui.Geometries;
using Mapsui.UI.Forms.Extensions;
using Xamarin.Forms;
using BruTile.Cache;
using System.Threading.Tasks;
using Acr.UserDialogs;
using System.IO;

namespace MapsuiTest
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        private readonly HttpClient httpClient;

        public MainPage()
        {
            InitializeComponent();

            downloadMapButton.Clicked += OnDownloadMapButtonClicked;

            httpClient = new HttpClient();

            Mapsui.Logging.Logger.LogDelegate += (level, message, ex) =>
            {
                Console.WriteLine($"[{level}]: {message}");
            };

            var map = new Map
            {
                CRS = "EPSG:3857",
                Transformation = new MinimalTransformation()
            };

            var readonlyCache = new ReadOnlyFileCache(
                GetCacheFolder(KartverketTileSource.SjøkartRaster), "png");
            var tileSource = KartverketSources.Create(
                KartverketTileSource.SjøkartRaster,
                FetchFactory.CreateRetryFetcher(httpClient),
                readonlyCache);

            var tileLayer = new TileLayer(tileSource)
            {
                Name = tileSource.Name,
            };
            map.Layers.Add(tileLayer);

            map.Widgets.Add(new ScaleBarWidget(map)
            {
                TextAlignment = Alignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom
            });

            map.DataChanged += OnMapDataChanged;

            var latLon = SphericalMercator.FromLonLat(22.88967, 70.114923);
            mapView.MyLocationEnabled = false;
            mapView.Map = map;
            mapView.UseDoubleTap = true;
            mapView.IsNorthingButtonVisible = false;
            mapView.IsMyLocationButtonVisible = false;
            mapView.IsZoomButtonVisible = false;
            mapView.Navigator.NavigateTo(latLon, ZoomLevelExtensions.ToMapsuiResolution(10));
            mapView.Refresh();
        }

        private void OnDownloadMapButtonClicked(object sender, EventArgs e)
        {
            Task.Run(DownloadMap);
        }

        private void DownloadMap()
        {
            Device.BeginInvokeOnMainThread(() => downloadMapButton.IsEnabled = false);

            var fileCache = new FileCache(GetCacheFolder(KartverketTileSource.SjøkartRaster), "png");
            var tileSource = KartverketSources.Create(
                KartverketTileSource.SjøkartRaster,
                FetchFactory.CreateRetryFetcher(httpClient),
                fileCache);

            var mapExtent = mapView.Viewport.Extent.ToExtent();
            var fetchStrategy = new DataFetchStrategy();
            var tileInfos = fetchStrategy.Get(tileSource.Schema, mapExtent, 15);

            using (var loading = UserDialogs.Instance.Loading())
            {
                int tileCounter = 0;
                foreach (var tileInfo in tileInfos)
                {
                    tileCounter++;
                    double progress = Math.Round(tileCounter / (float)tileInfos.Count * 100.0f, 2);

                    var tileSize = 0;
                    if (!fileCache.Exists(tileInfo.Index))
                    {
                        var tile = tileSource.GetTile(tileInfo);
                        tileSize = tile.Length;
                    }

                    var progressString = $"{progress}% ({tileCounter}/{tileInfos.Count})";
                    var progressMessage = $"Downloading\n{progressString}";
                    Console.WriteLine(
                        $"{progressString} - " +
                        $"tile col: {tileInfo.Index.Col}, " +
                        $"tile row: {tileInfo.Index.Row}, " +
                        $"tile level: {tileInfo.Index.Level}, " +
                        $"tile size {tileSize}");

                    loading.Title = progressMessage;
                }
            }

            Device.BeginInvokeOnMainThread(() => downloadMapButton.IsEnabled = true);
        }

        private void OnMapDataChanged(object sender, DataChangedEventArgs e)
        {
            if (e.Error != null)
            {
                Console.WriteLine($"Data fetch error: {e.Error.Message}");
            }
        }

        private string GetCacheFolder(KartverketTileSource source)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "tilecache", source.ToString());
            Console.WriteLine($"Path: {path}");
            return path;
        }
    }
}