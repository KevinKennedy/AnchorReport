﻿
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AnchorReport;
using System.Numerics;


namespace UnitTests
{
    [TestClass]
    public class UnitTest1
    {

        private static void AssertEqual(float expected, float actual)
        {
            const float epsilon = 0.00001f;

            if (Math.Abs(expected - actual) > epsilon)
            {
                throw new AssertFailedException($"AssertEqual failed.  Expected:{expected}    Actual:{actual}");
            }
        }

        [TestMethod]
        public void AnchorStatsSmoke()
        {
            var stats = new AnchorStats();

            string json = @"
                {
                ""Anchors"" :
                    [
                        {
                        ""GUID"" : ""{591167D0-BE59-4295-BD98-0DC44FE7410E}"",
                        ""Transform"" : [1,0,0,0,
                                         0,1,0,0,
                                         0,0,1,0,
                                         0,0,0,1]
                        }
                    ]
                }";

            // First sample should give zero volume
            var collection = new AnchorCollection(json);
            stats.AddSample(collection);
            AssertEqual(0.0f, stats.TotalMovementVolume);
            AssertEqual(0.0f, stats.MeanMovementVolume);

            // move the anchor so that the bounding box volume is 1 cubic unit
            collection.Anchors[0] = new Anchor(
                collection.Anchors[0].Guid,
                Matrix4x4.Multiply(collection.Anchors[0].Transform,
                    Matrix4x4.CreateTranslation(
                        new Vector3(1.0f, 1.0f, 1.0f))));
            stats.AddSample(collection);
            AssertEqual(1.0f, stats.TotalMovementVolume);
            AssertEqual(1.0f, stats.MeanMovementVolume);
        }

        [TestMethod]
        public void LoadBugRepro()
        {
            var stats = new AnchorStats();

            // Set of anchors we got when running an app.  Note Duplicate Anchors: 
            //      608C679E-F287-41C4-B3E7-0510E6A29893
            //      C19AA110-F0B2-48A9-A0A1-B3B3B3837AE9
            string json = @"
                {""Anchors"" : 
                [
                    {""GUID"" : ""{055DE45C-C050-4304-9358-B57DF76BCE45}"", ""Transform"" : [-0.995597,-0.000381,-0.093740,0.000000,-0.000343,1.000001,-0.000415,0.000000,0.093740,-0.000382,-0.995597,0.000000,-6.348280,-1.246484,3.385142,1.000000]},
                    {""GUID"" : ""{04FCEB61-8E9C-4BED-8B8D-5E1D07EBC481}"", ""Transform"" : [0.093680,-0.000486,-0.995603,0.000000,-0.001487,1.000000,-0.000628,0.000000,0.995602,0.001540,0.093679,0.000000,9.561540,-1.549089,4.026551,1.000000]},
                    {""GUID"" : ""{608C679E-F287-41C4-B3E7-0510E6A29893}"", ""Transform"" : [0.095441,-0.000165,-0.995436,0.000000,-0.001706,1.000000,-0.000329,0.000000,0.995434,0.001730,0.095441,0.000000,5.785565,1.882545,1.457783,1.000000]},
                    {""GUID"" : ""{54436FA9-36D8-44BD-A9CF-C1BCB6271213}"", ""Transform"" : [0.094895,-0.000137,-0.995488,0.000000,0.000831,1.000001,-0.000058,0.000000,0.995487,-0.000821,0.094895,0.000000,9.016687,-0.601529,3.981854,1.000000]},
                    {""GUID"" : ""{608C679E-F287-41C4-B3E7-0510E6A29893}"", ""Transform"" : [0.580259,-0.000338,-0.814433,0.000000,-0.000240,1.000001,-0.000586,0.000000,0.814433,0.000536,0.580259,0.000000,5.644509,-1.296789,0.652741,1.000000]},
                    {""GUID"" : ""{ECB19263-C086-4FA4-B211-DD218CD6CB44}"", ""Transform"" : [0.092571,-0.000802,-0.995706,0.000000,-0.000348,1.000000,-0.000837,0.000000,0.995707,0.000424,0.092571,0.000000,-7.373230,-0.744854,-0.362145,1.000000]},
                    {""GUID"" : ""{608C679E-F287-41C4-B3E7-0510E6A29893}"", ""Transform"" : [-0.095508,-0.000126,0.995429,0.000000,-0.001670,1.000000,-0.000033,0.000000,-0.995428,-0.001666,-0.095508,0.000000,7.351339,-0.676806,1.536312,1.000000]},
                    {""GUID"" : ""{C19AA110-F0B2-48A9-A0A1-B3B3B3837AE9}"", ""Transform"" : [0.094467,-0.001381,-0.995528,0.000000,-0.001519,0.999999,-0.001531,0.000000,0.995527,0.001657,0.094466,0.000000,-3.163381,0.040453,6.140144,1.000000]},
                    {""GUID"" : ""{D6B50E12-E1D1-4878-98E0-67D9809B727C}"", ""Transform"" : [0.093164,-0.001119,-0.995651,0.000000,-0.000983,1.000000,-0.001216,0.000000,0.995651,0.001092,0.093163,0.000000,9.771378,-0.312833,3.793792,1.000000]},
                    {""GUID"" : ""{7679447D-6796-43E1-BA2E-8AE5086023DF}"", ""Transform"" : [0.995508,0.000203,0.094678,0.000000,-0.000161,1.000001,-0.000450,0.000000,-0.094677,0.000433,0.995508,0.000000,0.518969,-0.582258,3.580719,1.000000]},
                    {""GUID"" : ""{C19AA110-F0B2-48A9-A0A1-B3B3B3837AE9}"", ""Transform"" : [0.094454,0.000562,-0.995530,0.000000,0.000060,1.000001,0.000570,0.000000,0.995530,-0.000113,0.094454,0.000000,-1.086296,-0.133457,5.597109,1.000000]},
                    {""GUID"" : ""{7DB986A2-B870-4807-8712-ADE432B9D029}"", ""Transform"" : [0.093630,-0.000004,-0.995608,0.000000,-0.000159,1.000001,-0.000019,0.000000,0.995607,0.000160,0.093630,0.000000,1.759000,-0.715894,-5.627509,1.000000]},
                    {""GUID"" : ""{F9EEEAFC-7896-4451-BF5D-41D93EC02AD1}"", ""Transform"" : [0.093636,-0.000284,-0.995607,0.000000,-0.000337,1.000001,-0.000316,0.000000,0.995607,0.000365,0.093636,0.000000,-5.594704,-0.497474,-5.301585,1.000000]},
                    {""GUID"" : ""{5BA045C9-E6BA-431D-A38B-1927B913CAB8}"", ""Transform"" : [0.094443,-0.000412,-0.995531,0.000000,0.000227,1.000001,-0.000392,0.000000,0.995531,-0.000189,0.094444,0.000000,0.271638,-0.506200,-0.289621,1.000000]},
                    {""GUID"" : ""{0981AC79-8C09-4904-9F51-142EC9998711}"", ""Transform"" : [0.092388,0.000025,-0.995724,0.000000,-0.001278,1.000000,-0.000093,0.000000,0.995723,0.001281,0.092388,0.000000,6.882591,-0.695996,5.806731,1.000000]},
                    {""GUID"" : ""{3637552C-EFD6-4494-B3C5-7389A925E1CB}"", ""Transform"" : [0.094715,-0.000343,-0.995505,0.000000,-0.000951,1.000000,-0.000434,0.000000,0.995504,0.000988,0.094715,0.000000,1.045143,-0.819825,5.829003,1.000000]},
                    {""GUID"" : ""{16A94077-1EC6-49D7-ACA1-54BE8309E57A}"", ""Transform"" : [0.460474,0.000409,-0.887674,0.000000,-0.000785,1.000001,0.000054,0.000000,0.887673,0.000673,0.460475,0.000000,1.025550,-1.716586,-5.415912,1.000000]},
                    {""GUID"" : ""{3B833867-EEA5-4BA0-8733-3B8E7E013077}"", ""Transform"" : [-0.052021,0.000425,0.998646,0.000000,-0.000176,1.000001,-0.000434,0.000000,-0.998646,-0.000199,-0.052021,0.000000,-7.823972,-1.678024,2.006373,1.000000]},
                    {""GUID"" : ""{608C679E-F287-41C4-B3E7-0510E6A29893}"", ""Transform"" : [0.095338,-0.000003,-0.995445,0.000000,-0.001388,1.000000,-0.000136,0.000000,0.995445,0.001395,0.095338,0.000000,4.780951,-1.623369,5.874389,1.000000]},
                    {""GUID"" : ""{A23BEE06-D8B9-4D7B-9D59-090515538665}"", ""Transform"" : [-0.019314,-0.001747,0.999813,0.000000,0.000742,0.999999,0.001762,0.000000,-0.999814,0.000775,-0.019313,0.000000,10.092489,-1.682046,-3.440515,1.000000]},
                    {""GUID"" : ""{E4679630-8DFE-486E-A690-1337AA03E36D}"", ""Transform"" : [0.995558,0.000712,0.094146,0.000000,-0.000719,1.000001,0.000038,0.000000,-0.094145,-0.000105,0.995559,0.000000,1.886716,-0.868102,3.774606,1.000000]}
                ]}";

            var collection = new AnchorCollection(json);
            stats.AddSample(collection);
            AssertEqual(0.0f, stats.TotalMovementVolume);
            AssertEqual(0.0f, stats.MeanMovementVolume);
            AssertEqual(2, stats.IgnoredAnchors.Length); // 2 guids above have multiple instances
        }
    }
}