using LiveCharts;
using LiveCharts.Defaults;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows;

namespace PingWPF
{
    public class PingModel : INotifyPropertyChanged
    {
        private const int MaxPingResults = 100; // Maximum number of ping results to store
        private const int PingIntervalMilliseconds = 1000; // Interval between pings in milliseconds

        private Queue<PingResult> pingResults; // Queue to store ping results
        private bool isRunning; // Flag to indicate if the ping application is running

        public event PropertyChangedEventHandler PropertyChanged;

        public PingModel()
        {
            pingResults = new Queue<PingResult>( MaxPingResults );
            isRunning = false;
            PingChartValues = new ChartValues<ObservablePoint>();
        }

        public ChartValues<ObservablePoint> PingChartValues { get; set; }

        public void StartPing( string ipAddress )
        {
            if ( isRunning )
            {
                MessageBox.Show( "Ping application is already running." );
                return;
            }

            isRunning = true;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += ( sender, e ) =>
            {
                using ( Ping ping = new Ping() )
                {
                    while ( isRunning )
                    {
                        try
                        {
                            PingReply reply = ping.Send(ipAddress);

                            DateTime currentTime = DateTime.Now;
                            string result = $"{currentTime}: {reply.RoundtripTime}ms";
                            PingResult pingResult = new PingResult(currentTime, reply.RoundtripTime);
                            pingResults.Enqueue( pingResult );

                            if ( pingResults.Count > MaxPingResults )
                            {
                                pingResults.Dequeue();
                            }

                            Application.Current.Dispatcher.Invoke( () =>
                            {
                                if (PingChartValues.Count + 1 > MaxPingResults )
                                {
                                    PingChartValues.RemoveAt( 0 );
                                    PingChartValues.Add( PassPing( pingResult ) );
                                }
                                else
                                {
                                    PingChartValues.Add(PassPing(pingResult) );
                                } 

                                OnPropertyChanged(nameof(PingChartValues));
                            } );
                        }
                        catch ( PingException ex )
                        {
                            MessageBox.Show( $"Ping exception: {ex.Message}" );
                        }

                        Thread.Sleep( PingIntervalMilliseconds );
                    }
                }
            };

            worker.RunWorkerAsync();
        }

        private ObservablePoint PassPing( PingResult pingResult )
        {
            double unixTimestamp = pingResult.Timestamp.ToUniversalTime().Subtract(new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day)).TotalMilliseconds;
            ObservablePoint result = new ObservablePoint(unixTimestamp, pingResult.RoundtripTime);
            return result;
        }

        public void StopPing()
        {
            if ( !isRunning )
            {
                MessageBox.Show( "Ping application is not running." );
                return;
            }

            isRunning = false;
        }

        private void UpdateChartValues()
        {
            PingChartValues.Clear();

            foreach ( PingResult result in pingResults )
            {
                double unixTimestamp = result.Timestamp.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
                ObservablePoint dataPoint = new ObservablePoint(unixTimestamp, result.RoundtripTime);
                PingChartValues.Add( dataPoint );
            }

            OnPropertyChanged( nameof( PingChartValues ) );
        }

        protected void OnPropertyChanged( string propertyName )
        {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        }
    }
}
