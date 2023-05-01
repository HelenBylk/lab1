using System;
using System.Data;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;


namespace ClassLibrary
{
    public delegate double FRaw(double x);
    public enum FRawEnum
    {
        Linear,
        Cubic,
        Random
    };
    public class RawData
    {
        public static readonly Dictionary<FRawEnum, FRaw> MethodsFRaw = new()
        {
            { FRawEnum.Linear, Linear },
            { FRawEnum.Cubic, Cubic },
            { FRawEnum.Random, Random }
        };
        public double ABorder { get; set; }
        public double BBorder { get; set; }
        public int NodesCount { get; set; }
        public bool IsUniform { get; set; }
        [JsonIgnore]
        public FRaw RawFunc { get; set; }
        public double[] Nodes { get; set; }
        public double[] Values { get; set; }

        public RawData(double aBorder, double bBorder, int nodesCount, bool isUniform, FRaw func)
        {
            ABorder = aBorder;
            BBorder = bBorder;
            NodesCount = nodesCount;
            IsUniform = isUniform;
            RawFunc = func;
            Nodes = new double[NodesCount];
            Values = new double[NodesCount];
            double step = (BBorder - ABorder) / (NodesCount - 1);
            for (int i = 0; i < NodesCount; i++)
            {
                Nodes[i] = ABorder + step * i;
                Values[i] = RawFunc(Nodes[i]);
            }
        } 
        public RawData(string fileName)
        {
            StreamReader streamReader = new StreamReader(fileName);
            try
            {
                JsonObject jsonRawData = JsonNode.Parse(streamReader.ReadToEnd()).AsObject();
                FRawEnum fRawEnum = Enum.Parse<FRawEnum>(jsonRawData[nameof(RawFunc)].GetValue<string>());
                ABorder = jsonRawData[nameof(ABorder)].GetValue<double>();
                BBorder = jsonRawData[nameof(BBorder)].GetValue<double>();
                NodesCount = jsonRawData[nameof(NodesCount)].GetValue<int>();
                IsUniform = jsonRawData[nameof(IsUniform)].GetValue<bool>();
                RawFunc = MethodsFRaw[fRawEnum];

                Nodes = new double[NodesCount];
                Values = new double[NodesCount];

                for(int i = 0; i < NodesCount; i++)
                {
                    Nodes[i] = jsonRawData[nameof(Nodes)][i].GetValue<double>();
                    Values[i] = jsonRawData[nameof(Values)][i].GetValue<double>();
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            finally
            {
                streamReader.Close();
            }
        }
        public static double Linear(double x) => x;
        public static double Cubic(double x) => Math.Pow(x, 3);
        public static double Random(double x) => (double)RandomNumberGenerator.GetInt32(-1000, 1000) / 1000;
        public void Save(string filename)
        {
            StreamWriter streamWriter = new(new FileStream(filename, FileMode.Create));
            try
            {
                string serializedInstance = JsonSerializer.Serialize(this);
                JsonObject json = JsonNode.Parse(serializedInstance).AsObject();
                json.Add(nameof(RawFunc), RawFunc.Method.Name);
                streamWriter.WriteLine(json.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                streamWriter.Close();
            }
        }
        public static void Load(string fileName, out RawData rawData)
        {
            rawData = new RawData(fileName);
        }
    };
    public struct SplineDataItem
    {
        public double Coordinate { get; set; }
        public double[] CoordinateValues { get; set; }
        public SplineDataItem(double coordinate, double[] coordinateValues)
        {
            Coordinate = coordinate;
            CoordinateValues = coordinateValues;
        }
        public string ToString(string format)
        {
            return $"{Coordinate.ToString(format)}  " +
                $"{CoordinateValues[0].ToString(format)}  " +
                $"{CoordinateValues[1].ToString(format)}  " +
                $"{CoordinateValues[2].ToString(format)}";
        }
        public override string ToString()
        {
            return string.Format(
                "Coordinate = {0:F3},\n" +
                "Spline value = {1:F5},\n" +
                "Spline 1st derivative = {2:F5},\n" +
                "Spline 2nd derivative = {3:F5};\n",
                Coordinate,
                CoordinateValues[0],
                CoordinateValues[1],
                CoordinateValues[2]
            );
        }
    };
    public class SplineData
    {
        public RawData rawData { get; set; }
        public double[] BoundaryConditions { get; set; }
        public int NodesCount { get; set; }
        public List<SplineDataItem> Values { get; set; }
        public double Integral { get; set; }
        public SplineData(RawData rawdata, double[] boundaryConditions, int nodesCount)
        {
            rawData = rawdata;
            NodesCount = nodesCount;
            Values = new List<SplineDataItem>();
            BoundaryConditions = boundaryConditions;
        }
        [DllImport("D:\\sem6\\Solution2\\x64\\Debug\\Dll.dll")]
        private static extern void calculate(
            double[] nodes,
            int nodes_count,
            bool is_uniform,
            double[] values,
            double[] boundary_conditions,
            int rawdata_nodes,
            double[] borders,
            double[,] vals,
            ref double integral
        );
        public void CalculateSpline()
        {
            Values.Clear();
            double integral = 0;
            double[] borders = new double[2] { rawData.ABorder, rawData.BBorder };
            double[,] vals = new double[NodesCount, 3];
            double[] nodesToCalc = rawData.Nodes;
            if (rawData.IsUniform)
                nodesToCalc = borders;
            calculate(
                nodesToCalc,
                rawData.NodesCount,
                rawData.IsUniform,
                rawData.Values,
                BoundaryConditions,
                NodesCount,
                borders,
                vals,
                ref integral
            );
            double step = (rawData.BBorder - rawData.ABorder) / (NodesCount - 1);
            for (int i = 0; i < NodesCount; i++)
            {
                double[] tmp = Enumerable.Range(0, 3).Select(x => vals[i, x]).ToArray();
                double coord = rawData.ABorder + step * i;
                Values.Add(new SplineDataItem(coord, tmp));
            }
            Integral = integral;
        }
    };
}