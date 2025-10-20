using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SmartRefillWasm.Models
{
    public class RefillCalculationResult : INotifyPropertyChanged
    {
        private string _productName = "";
        public string ProductName
        {
            get => _productName;
            set => SetField(ref _productName, value);
        }

        private int _totalSales;
        public int TotalSales
        {
            get => _totalSales;
            set => SetField(ref _totalSales, value, Recalculate);
        }

        private decimal _adjustmentRate;
        public decimal AdjustmentRate
        {
            get => _adjustmentRate;
            set => SetField(ref _adjustmentRate, value, Recalculate);
        }
        
        private int _manualAdjustment;
        public int ManualAdjustment
        {
            get => _manualAdjustment;
            set => SetField(ref _manualAdjustment, value, Recalculate);
        }
        
        private int _maxCapacity;
        public int MaxCapacity
        {
            get => _maxCapacity;
            set => SetField(ref _maxCapacity, value, Recalculate);
        }

        private int _boxSize;
        public int BoxSize
        {
            get => _boxSize;
            set => SetField(ref _boxSize, value, Recalculate);
        }

        public int SmartAdjustment { get; private set; }
        public int FinalRecommendation { get; private set; }
        public int BoxesToTake { get; private set; }
        public int RemainingPieces { get; private set; }
        
        public void Recalculate()
        {
            // 智能调整逻辑：正常情况下是基础补货 × 调整率
            SmartAdjustment = (int)Math.Round(TotalSales * AdjustmentRate);
            
            // 容量约束：如果基础补货 > 最大容量，智能调整变为负数来适应系统
            if (MaxCapacity > 0 && TotalSales > MaxCapacity)
            {
                SmartAdjustment = MaxCapacity - TotalSales; // 这会是负数
            }
            
            var recommended = TotalSales + SmartAdjustment + ManualAdjustment;
            FinalRecommendation = recommended; // 允许负数结果，由用户判断 - 修复版本2.0
            // Debug: 记录计算过程
            Console.WriteLine($"[DEBUG] {ProductName}: TotalSales={TotalSales}, SmartAdjustment={SmartAdjustment}, ManualAdjustment={ManualAdjustment}, FinalRecommendation={FinalRecommendation}");

            if (BoxSize > 0)
            {
                if (FinalRecommendation >= 0)
                {
                    // 正数：正常计算箱数和余件
                    BoxesToTake = FinalRecommendation / BoxSize;
                    RemainingPieces = FinalRecommendation % BoxSize;
                }
                else
                {
                    // 负数：显示负数，箱数和余件都设为0
                    BoxesToTake = 0;
                    RemainingPieces = 0;
                }
            }
            else
            {
                BoxesToTake = 0;
                RemainingPieces = 0;
            }

            OnPropertyChanged(nameof(SmartAdjustment));
            OnPropertyChanged(nameof(FinalRecommendation));
            OnPropertyChanged(nameof(BoxesToTake));
            OnPropertyChanged(nameof(RemainingPieces));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, Action? onChanged = null, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            onChanged?.Invoke();
            return true;
        }
    }
} 