using CommonModel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PatientDataBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            using (HttpClient client = new HttpClient())
            {
                Task<HttpResponseMessage> t = client.GetAsync(URI + "use_cases");
                t.Wait();

                Task<string> t2 = t.Result.Content.ReadAsStringAsync();
                t2.Wait();

                var json = t2.Result;
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                foreach(var pair in result)
                {
                    UseCases.Items.Add(pair.Value);
                    myUseCases.Add(int.Parse(pair.Key), pair.Value);
                }

                UseCases.SelectedIndex = 0;
            }
            
        }

        private const string URI = @"http://localhost:52288/patients/";

        private Dictionary<int, string> myUseCases = new Dictionary<int, string>();

        private int mySelectedUseCase = 0;

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            PrepareResults();

            foreach (var pair in myUseCases)
            {
                if (pair.Value == UseCases.SelectedItem.ToString())
                {
                    using (HttpClient client = new HttpClient())
                    {
                        mySelectedUseCase = pair.Key;

                        Task<HttpResponseMessage> t = client.GetAsync(URI + "use_case_result?useCaseId=" + pair.Key);
                        t.Wait();

                        Task<string> t2 = t.Result.Content.ReadAsStringAsync();
                        t2.Wait();

                        var json = t2.Result;
                        var result = JsonConvert.DeserializeObject<List<Measurement>>(json);

                        int i = 1;
                        foreach (var measurement in result)
                        {
                            PatientResults.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                            TextBlock resultDateText = new TextBlock
                            {
                                Margin = new Thickness(5),
                                Text = measurement.MeasurementDate,
                                HorizontalAlignment = HorizontalAlignment.Center
                            };

                            TextBlock resultText = new TextBlock
                            {
                                Margin = new Thickness(5),
                                Text = measurement.MeasurementValue.ToString(CultureInfo.InvariantCulture),
                                HorizontalAlignment = HorizontalAlignment.Center
                            };

                            Button sendNotfication = new Button()
                            {
                                Margin = new Thickness(3),
                                Padding = new Thickness(2),
                                Content = "Send notification",
                                Tag = measurement.MeasurementId.ToString(),
                                HorizontalAlignment = HorizontalAlignment.Center
                            };
                            sendNotfication.Click += SendNotification;


                            PatientResults.Children.Add(resultDateText);
                            PatientResults.Children.Add(resultText);

                            Grid.SetColumn(resultDateText, 0);
                            Grid.SetColumn(resultText, 1);
                            Grid.SetRow(resultDateText, i);
                            Grid.SetRow(resultText, i);

                            if (measurement.IsProblem)
                            {
                                PatientResults.Children.Add(sendNotfication);
                                Grid.SetRow(sendNotfication, i);
                                Grid.SetColumn(sendNotfication, 3);
                            }

                            i++;
                        }

                    }


                    return;
                }
            }
        }

        private void PrepareResults()
        {
            PatientResults.Children.Clear();
            PatientResults.RowDefinitions.Clear();
            PatientResults.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock header1 = new TextBlock
            {
                Margin = new Thickness(5),
                FontWeight = FontWeights.Bold,
                Text = "Date",
                HorizontalAlignment = HorizontalAlignment.Center
            };

            TextBlock header2 = new TextBlock
            {
                Margin = new Thickness(5),
                FontWeight = FontWeights.Bold,
                Text = "Measurement",
                HorizontalAlignment = HorizontalAlignment.Center
            };

            TextBlock header3 = new TextBlock
            {
                Margin = new Thickness(5),
                FontWeight = FontWeights.Bold,
                Text = "Suspicious",
                HorizontalAlignment = HorizontalAlignment.Center
            };

            PatientResults.Children.Add(header1);
            PatientResults.Children.Add(header2);
            PatientResults.Children.Add(header3);

            Grid.SetColumn(header1, 0);
            Grid.SetColumn(header2, 1);
            Grid.SetColumn(header3, 2);
            Grid.SetRow(header1, 0);
            Grid.SetRow(header2, 0);
            Grid.SetRow(header3, 0);
        }

        private void SendNotification(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            using (HttpClient client = new HttpClient())
            {
                Task<HttpResponseMessage> t = client.GetAsync(URI + $"send_notification?useCaseId={mySelectedUseCase}&measurementId={button.Tag}");
                t.Wait();
            }
        }
    }
}
