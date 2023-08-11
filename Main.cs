using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using TradingPlatform.BusinessLayer;
using System.Text;
using System.Linq;

namespace Background_Alert_Quantower
{
    internal static class Main
    {
        private static bool isRun;
        private static bool isConnected;
        private static bool isLoaded;

        private static List<Symbol> Symbols;
        private static List<Alert> Alerts;

#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
        [ModuleInitializer]
#pragma warning restore CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
        internal static void Init() => Run();

        internal static void Run()
        {
            if (isRun) return;
            isRun = true;
            Symbols = new();
            Alerts = new();
            Core.Instance.Connections.ConnectionStateChanged += Connections_ConnectionStateChanged;
            Loop();
        }

        private static void Connections_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {


            if (e.NewState == ConnectionState.Connected)
            {
                if (!isLoaded)
                {
                    isLoaded = true;
                    Load();
                }
                isConnected = true;
            }
            else
            {
                isConnected = false;
            }
        }

        internal static void Add(Symbol symbol, Alert alert)
        {
            if (!isConnected) return;
            int index = Symbols.FindIndex(x => x.Name == symbol.Name);
            if (index == -1)
                Symbols.Add(symbol);
            index = Alerts.FindIndex(x => x != null && x.Id == alert.Id);
            if (index == -1)
                Alerts.Add(alert);
            else
                Alerts[index] = alert;
            Save();
        }

        internal static void Remove(string id, string symbol = null, double price = 0)
        {
            int index = Alerts.FindIndex(x => x != null && x.Id == id);
            if (index == -1)
                index = Alerts.FindIndex(x => x != null && x.Symbol == symbol && x.Price == price);
            Alerts[index] = null;
            Save();
        }

        private static void Remove(int index)
        {
            Alerts[index] = null;
            Save();
        }

        internal static void Move(string id, double price, bool isUnder)
        {
            int index = Alerts.FindIndex(x => x != null && x.Id == id);
            if (index == -1) return;
            Alerts[index].Price = price;
            Alerts[index].IsUnder = isUnder;
            Save();
        }

        private async static void Loop()
        {
            while (true)
            {
                await Task.Delay(350);
                for (int i = 0; i < Alerts.Count; i++)
                {
                    Alert alert = Alerts[i];
                    if (alert == null) continue;
                    Symbol symbol = Symbols.Find(x => x.Name == alert.Symbol);
                    if (symbol == null) continue;
                    if (alert.IsUnder)
                    {
                        if (symbol.Last >= alert.Price)
                        {
                            alert.IsUnder = false;
                            string description = symbol.Name.Replace(symbol.QuotingCurrency.Name, "") + ", Price: " + alert.Price.ToString() + ", REACHED.";
                            Core.Instance.Alert(description, symbol.Name);
                            if (alert.AfterExecution != "None")
                                Remove(i);
                        }
                    }
                    else
                    {
                        if (symbol.Last < alert.Price)
                        {
                            alert.IsUnder = true;
                            string description = symbol.Name.Replace(symbol.QuotingCurrency.Name, "") + ", Price: " + alert.Price.ToString() + ", REACHED.";
                            Core.Instance.Alert(description, symbol.Name);
                            if (alert.AfterExecution != "None")
                                Remove(i);
                        }
                    }
                }
            }
        }

        private static void Save()
        {
            Task.Run(async () =>
            {
                await Task.Delay(100);
                string path = "../../../Settings/Scripts/Indicators/Background_Alert_Quantower/";
                bool flag = Path.Exists(path + "Background_Alert_Quantower.dll");
                if (flag)
                    path += "Alerts.json";
                else
                    path = "Alerts.json";
                string json = "";
                if (Alerts != null && Alerts.Count > 0)
                    json = JsonSerializer.Serialize(Alerts);
                File.WriteAllText(path, json, Encoding.UTF8);
            });
        }

        private static void Load()
        {
            Task.Run(async () =>
            {
                await Task.Delay(100);
                string path = "../../../Settings/Scripts/Indicators/Background_Alert_Quantower/";
                bool flag = Path.Exists(path + "Background_Alert_Quantower.dll");
                if (flag)
                    path += "Alerts.json";
                else
                    path = "Alerts.json";
                flag = Path.Exists(path);
                if (!flag) return;
                string json = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrEmpty(json)) return;
                List<Alert> alerts = JsonSerializer.Deserialize<List<Alert>>(json);
                await Task.Delay(1000);
                for (int i = 0; i < alerts.Count; i++)
                {
                    if (alerts[i] == null) continue;
                    Symbol symbol = Core.Instance.Symbols.First(x => x.Name == alerts[i].Symbol);
                    if (symbol != null)
                    {
                        Add(symbol, alerts[i]);
                        alerts[i] = null;
                    }
                }
            });
        }
    }
}
