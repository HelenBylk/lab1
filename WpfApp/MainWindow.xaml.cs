using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using ClassLibrary;


namespace WpfApp
{
    [ValueConversion(typeof(string), typeof(double[]))]
    public class DoubleArrayToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double[] doublies = (double[])value;
            return $"{doublies[0]} {doublies[1]}";
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string text = (string)value;
            string[] doubliesString = text.Split(" ");
            double[] doublies = new double[2];
            try
            {
                doublies[0] = System.Convert.ToDouble(doubliesString[0]);
                doublies[1] = System.Convert.ToDouble(doubliesString[1]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return doublies;
        }
    }
    public class ViewData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private double _aBorder { get; set; } = 0;
        public double ABorder
        {
            get => _aBorder;
            set
            {
                _aBorder = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ABorder)));
            }
        }
        private double _bBorder { get; set; } = 5;
        public double BBorder
        {
            get => _bBorder;
            set
            {
                _bBorder = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BBorder)));
            }
        }
        private int _nodesCount { get; set; } = 10;
        public int NodesCount
        {
            get => _nodesCount;
            set
            {
                _nodesCount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NodesCount)));
            }
        }
        private bool _isUniform { get; set; } = true;
        public bool IsUniform
        {
            get => _isUniform;
            set
            {
                _isUniform = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsUniform)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsNonUniform)));
            }
        }
        public bool IsNonUniform
        {
            get => !_isUniform;
            set
            {
                _isUniform = !value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsUniform)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsNonUniform)));
            }
        }
        private FRaw _functionName { get; set; } = RawData.Linear;
        public FRawEnum FunctionName
        {
            get => Enum.Parse<FRawEnum>(_functionName.Method.Name);
            set
            {
                _functionName = RawData.MethodsFRaw[value];
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FunctionName)));
            }
        }
        public int _splineNodesCount { get; set; } = 10;
        public int SplineNodesCount
        {
            get => _splineNodesCount;
            set
            {
                _splineNodesCount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SplineNodesCount)));
            }
        }
        private double[] _boundaryConditions { get; set; } = { 0, 0 };
        public double[] BoundaryConditions
        {
            get => _boundaryConditions;
            set
            {
                _boundaryConditions = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BoundaryConditions)));
            }
        }
        public RawData RawData;
        public SplineData SplineData;
        public ViewData() 
        {
            RawData = new RawData(_aBorder, _bBorder, _nodesCount, _isUniform, _functionName);
            SplineData = new SplineData(RawData, _boundaryConditions, _splineNodesCount);
        }
        public void UpdateDatas()
        {
            RawData = new RawData(_aBorder, _bBorder, _nodesCount, _isUniform, _functionName);
            SplineData = new SplineData(RawData, _boundaryConditions, _splineNodesCount);
        }
        public void Save(string filename)
        {
            RawData = new RawData(_aBorder, _bBorder, _nodesCount, _isUniform, _functionName);
            RawData.Save(filename);
        }
        public void Load(string filename)
        {
            try
            {
                RawData rawDataFromFile;
                RawData.Load(filename, out rawDataFromFile);
                ABorder = rawDataFromFile.ABorder;
                BBorder = rawDataFromFile.BBorder;
                NodesCount = rawDataFromFile.NodesCount;
                IsUniform = rawDataFromFile.IsUniform;
                FunctionName = Enum.Parse<FRawEnum>(rawDataFromFile.RawFunc.Method.Name);
                RawData = rawDataFromFile;
                SplineData = new(RawData, _boundaryConditions, _splineNodesCount);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }
    public partial class MainWindow : Window
    {
        public ViewData viewData = new ViewData();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = viewData;
            functionSelectorComboBox.ItemsSource = Enum.GetValues(typeof(FRawEnum));
        }
        private void SaveRawData(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            try
            {
                if (dialog.ShowDialog() != true)
                    return;
                viewData.Save(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void CalculateRawDataFromFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            try
            {
                if (dialog.ShowDialog() != true)
                    return;
                viewData.Load(dialog.FileName);
                Calculate(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void CalculateRawDataFromControls(object sender, RoutedEventArgs e) 
        { 
            try
            {
                viewData.UpdateDatas();
                Calculate(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void Calculate(object sender, RoutedEventArgs e)
        {
            try
            {
                viewData.SplineData.CalculateSpline();
                rawDataNodesListBox.Items.Clear();
                for (int i = 0; i < viewData.NodesCount; i++)
                {
                    rawDataNodesListBox.Items.Add(string.Format(
                        "Coordinate = {0:F3},  Value = {1:F3};",
                        viewData.RawData.Nodes[i],
                        viewData.RawData.Values[i]
                    ));
                }
                integralTextBlock.Text = viewData.SplineData.Integral.ToString("0.000");
                splineDataItemsListBox.ItemsSource = viewData.SplineData.Values;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }
    }
}
