using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Chart;
using System.Threading.Tasks;

namespace BackgroundAlert
{
    public class BackgroundAlert : Indicator
    {
        public BackgroundAlert() : base()
        {
            Name = "BackgroundAlert";
            Description = "All alerts will be enabled in background, even if you close chart.";
            SeparateWindow = false;
        }

        protected override void OnInit()
        {
            base.OnInit();
            Main.Run();
            CurrentChart.Drawings.Added += this.Drawings_Added;
            CurrentChart.Drawings.Removed += this.Drawings_Removed;
            CurrentChart.Drawings.Moved += this.Drawings_Moved;
        }

        protected override void OnClear()
        {
            base.OnClear();
            CurrentChart.Drawings.Added -= this.Drawings_Added;
            CurrentChart.Drawings.Removed -= this.Drawings_Removed;
            CurrentChart.Drawings.Moved -= this.Drawings_Moved;
        }

        private void Drawings_Added(DrawingEventArgs obj)
        {
            if (!obj.Drawing.Type.ToString().Equals("AlertHorizontalLine")) return;
            Task.Run(() => {
                SettingItem priceSetting = obj.Drawing.Settings.GetItemByName("Price");
                if (priceSetting == null) return;
                double price = (double)priceSetting.Value;
                bool isUnder = this.Symbol.Last < price;
                SettingItem afterExecutionSetting = obj.Drawing.Settings.GetItemByName("PostExecutionType");
                if (afterExecutionSetting == null) return;
                Main.Add(Symbol, new()
                {
                    Id = obj.Drawing.Id,
                    Symbol = this.Symbol.Name,
                    Price = price,
                    IsUnder = isUnder,
                    AfterExecution = afterExecutionSetting.Value.ToString()
                });
            });
        }

        private void Drawings_Removed(DrawingEventArgs obj)
        {
            if (!obj.Drawing.Type.ToString().Equals("AlertHorizontalLine")) return;
            Task.Run(() =>
            {
                SettingItem priceSetting = obj.Drawing.Settings.GetItemByName("Price");
                if (priceSetting != null)
                {
                    double price = (double)priceSetting.Value;
                    Main.Remove(obj.Drawing.Id, Symbol.Name, price);
                }
                else
                {
                    Main.Remove(obj.Drawing.Id);
                }
            });
        }

        private void Drawings_Moved(DrawingEventArgs obj)
        {
            if (!obj.Drawing.Type.ToString().Equals("AlertHorizontalLine")) return;
            Task.Run(async () =>
            {
                await Task.Delay(100);
                SettingItem priceSetting = obj.Drawing.Settings.GetItemByName("Price");
                if (priceSetting == null) return;
                double price = (double)priceSetting.Value;
                bool isUnder = this.Symbol.Last < price;
                Main.Move(obj.Drawing.Id, price, isUnder);
            });
        }
    }
}
