using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SMRView.Controller;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MessageBox.Avalonia.Enums;

namespace SMRView
{
    public class MainWindow : Window
    { 
        private readonly string NORMAL = "Normale";
        private readonly string BYZANTINE = "Bizantina";
        private int j = 0;
        private readonly List<TextBox> textBoxes;
        private static readonly ControllerPersona controllerPersona = new ControllerPersona();
        private ControllerPersona _controllerPersona = controllerPersona;
        private TextBox person;
        private TextBox nameBox;
        private TextBox surNameBox;
        private TextBox etaBox;
        private TextBox idpersonBox;
        private TextBox ReqB;
        private TextBox Replica0;
        private TextBox Replica1;
        private TextBox Replica2;
        private TextBox Replica3;
        private Button ReplicaButton0;
        private Button ReplicaButton1;
        private Button ReplicaButton2;
        private Button ReplicaButton3;
        private CheckBox haMachinaBox;
        /// <include file="Docs/MainWindow.xml" path='docs/members[@name="mainwndow"]/MainWindowC/*'/>
        public MainWindow()
        {
            InitializeComponent();

            textBoxes = new() { Replica0, Replica1, Replica2, Replica3 };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            person = this.FindControl<TextBox>("person");
            nameBox = this.FindControl<TextBox>("nameBox"); 
            surNameBox = this.FindControl<TextBox>("surNameBox"); 
            etaBox = this.FindControl<TextBox>("etaBox"); 
            idpersonBox = this.FindControl<TextBox>("idpersonBox"); 
            ReqB = this.FindControl<TextBox>("ReqB"); 
            Replica0 = this.FindControl<TextBox>("Replica0"); 
            Replica1 = this.FindControl<TextBox>("Replica1"); 
            Replica2 = this.FindControl<TextBox>("Replica2"); 
            Replica3 = this.FindControl<TextBox>("Replica3"); 
            haMachinaBox = this.FindControl<CheckBox>("haMachinaBox");
            ReplicaButton0 = this.FindControl<Button>("ReplicaButton0");
            ReplicaButton1 = this.FindControl<Button>("ReplicaButton1");
            ReplicaButton2 = this.FindControl<Button>("ReplicaButton2");
            ReplicaButton3 = this.FindControl<Button>("ReplicaButton3");
        }

        private async void Read_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(idpersonBox.Text, out int result))
            {
                GeneralResponses();
                _controllerPersona.ReadResponse += (sender, e) =>
                { 
                    e.ToList().ForEach(x =>
                    {
                        textBoxes.Find(xx => xx.Tag.ToString() == x.Key.ToString()).Text = x.Value.ToString();
                    });
                };

                async Task callGrpc()
                {
                    await Task.Delay(10);
                    await _controllerPersona.Read(result);
                }
                await Call(callGrpc);
                ReqB.Text = string.Empty;
                ReqB.Text = j.ToString();
                j++;
                await Task.Delay(5);
            }
        }
        private async Task Call(Func<Task> call)
        {
            try
            {
                await call();
            }
            catch (Exception e)
            {
                _controllerPersona = new ControllerPersona();
                var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager
                .GetMessageBoxStandardWindow("Client", e.ToString(),icon:MessageBox.Avalonia.Enums.Icon.Warning,
                style:Style.UbuntuLinux);
                await messageBoxStandardWindow.Show();
                
            }
        }
        private async void ReadAll_Click(object sender, RoutedEventArgs e)
        {
            GeneralResponses();
            EventWriteOperation();
            async Task callGrpc()
            {
                await _controllerPersona.ReadAll();
            }
            await Call(callGrpc);

        }
        private void GeneralResponses()
        {
            _controllerPersona.GeneralResponses += (sender, e) =>
            {
                if (!e.Item2)
                {
                    ShowDialogResult();
                }
                else
                {
                    person.Text = e.Item1;
                }
            };
        }
        private static async void ShowDialogResult()
        { 
            var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager
            .GetMessageBoxStandardWindow("Client", "Richiesta non completata con successo",
            icon: MessageBox.Avalonia.Enums.Icon.Error, style: Style.UbuntuLinux);
            await messageBoxStandardWindow.Show();
       
        }
        private void EventWriteOperation()
        {
            _controllerPersona.ReadAllResponses += (sender, e) =>
            {
                e.ToList().ForEach(x =>
                {
                    textBoxes.Find(xx => xx.Tag.ToString() == x.Key.ToString()).Text = string.Join($"\n", x.Value.Select(person => person.ToString()).ToArray());
                });

            };

        }
        private async void Insert_Click(object sender, RoutedEventArgs e)
        {
            var id = int.TryParse(idpersonBox.Text, out int res) ? res : 0;
            if (int.TryParse(etaBox.Text, out int eta))
            {
                GeneralResponses();
                EventWriteOperation();
                async Task callGrpc()
                {
                    await _controllerPersona.Insert(CreateUpdatePersona(id, j));
                }
                await Call(callGrpc);
                j++;
                ReqB.Text = string.Empty;
                ReqB.Text = j.ToString();
                await Task.Delay(5);
            }
        }
        private ZyzzyvagRPC.Services.PersonagRPC CreateUpdatePersona(int id, int eta) => new ZyzzyvagRPC.Services.PersonagRPC
        {
            Id = id,
            Nome = nameBox.Text,
            Cognome = surNameBox.Text,
            Eta = eta,
            HaMacchina = haMachinaBox.IsChecked.Value
        };
        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(idpersonBox.Text, out int result) && int.TryParse(etaBox.Text, out int eta))
            {
                GeneralResponses();
                EventWriteOperation();
                async Task callGrpc()
                {
                    await _controllerPersona.Update(CreateUpdatePersona(result, eta));
                }
                await Call(callGrpc);
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(idpersonBox.Text, out int result))
            {
                GeneralResponses();
                EventWriteOperation();
                async Task callGrpc()
                {
                    await _controllerPersona.Delete(result);
                }
                await Call(callGrpc);
            }
        }


        


        private async Task Call2(Func<Task> call)
        {
            try
            {
                await call();
            }
            catch (Exception e)
            {
                
                _controllerPersona = new ControllerPersona();
                var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager
                .GetMessageBoxStandardWindow("Client", e.ToString(), icon: MessageBox.Avalonia.Enums.Icon.Warning,
                style: Style.UbuntuLinux);
                await messageBoxStandardWindow.Show();
            }
        }

        private async void Replica3Byzantine(object sender, RoutedEventArgs e)
        {
            async Task callGrpc()
            {
                var x = await _controllerPersona.Byzantine(3);
                ReplicaButton3.Content = x.Byzantine ? BYZANTINE : NORMAL;
            }
            await Call2(callGrpc);
        }

        private async void Replica1Byzantine(object sender, RoutedEventArgs e)
        {
            async Task callGrpc()
            {
                var x = await _controllerPersona.Byzantine(1);
                ReplicaButton1.Content = x.Byzantine ? BYZANTINE : NORMAL;
            }
            await Call2(callGrpc);
        }

        private async void Replica2Byzantine(object sender, RoutedEventArgs e)
        {
            async Task callGrpc()
            {
                var x = await _controllerPersona.Byzantine(2);
                ReplicaButton2.Content = x.Byzantine ? BYZANTINE : NORMAL;
            }
            await Call2(callGrpc);
        }

        private async void Replica0Byzantine(object sender, RoutedEventArgs e)
        {
            async Task callGrpc()
            {
                var x = await _controllerPersona.Byzantine(0);
                ReplicaButton0.Content = x.Byzantine ? BYZANTINE : NORMAL;
            }
            await Call2(callGrpc);
        }

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            await _controllerPersona.DisposeAsync();
        }
    }
}
