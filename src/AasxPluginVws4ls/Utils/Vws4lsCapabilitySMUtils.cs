using AasCore.Aas3_0;
using NPOI.SS.Formula.Eval;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxPluginVws4ls;

public static class Vws4lsCapabilitySMUtils
{
    public const string SEM_ID_CAP_CUT = "https://arena2036.de/vws4ls/capability/1/0/CutCapability";
    public const string SEM_ID_CAP_CUTWIRE = "https://arena2036.de/vws4ls/capability/1/0/CutWireCapability";
    public const string SEM_ID_CAP_CUTTUBE = "https://arena2036.de/vws4ls/capability/1/0/CutTubeCapability";
    public const string SEM_ID_CAP_STRIP = "https://arena2036.de/vws4ls/capability/1/0/StripCapability";
    public const string SEM_ID_CAP_SLIT = "https://arena2036.de/vws4ls/capability/1/0/SlitCapability";
    public const string SEM_ID_CAP_CRIMP = "https://arena2036.de/vws4ls/capability/1/0/CrimpCapability";
    public const string SEM_ID_CAP_MARKWIRE = "https://arena2036.de/vws4ls/capability/1/0/MarkWireCapability";
    public const string SEM_ID_CAP_TINNING = "https://arena2036.de/vws4ls/capability/1/0/TinningCapability";
    public const string SEM_ID_CAP_REFINEMENTOFCABLELUGS = "https://arena2036.de/vws4ls/capability/1/0/RefinementOfCableLugsCapability";
    public const string SEM_ID_CAP_SEAL = "https://arena2036.de/vws4ls/capability/1/0/SealCapability";
    public const string SEM_ID_CAP_SLEEVE = "https://arena2036.de/vws4ls/capability/1/0/SleeveCapability";
    public const string SEM_ID_CAP_STRANDTWIST = "https://arena2036.de/vws4ls/capability/1/0/StrandTwistCapability";
    public const string SEM_ID_CAP_SHIELDTWIST= "https://arena2036.de/vws4ls/capability/1/0/ShieldTwistCapability";
    public const string SEM_ID_CAP_BLOCKLOAD = "https://arena2036.de/vws4ls/capability/1/0/BlockloadCapability";
    public const string SEM_ID_CAP_CONNECTORANDHOUSING = "https://arena2036.de/vws4ls/capability/1/0/ConnectorAndHousingCapability";
    public const string SEM_ID_CAP_ULTRASONICWELD = "https://arena2036.de/vws4ls/capability/1/0/UltraSonicWeldCapability";
    public const string SEM_ID_CAP_TERMINAL = "https://arena2036.de/vws4ls/capability/1/0/TerminalCapability";
    public const string SEM_ID_CAP_COVER = "https://arena2036.de/vws4ls/capability/1/0/CoverCapability";
    public const string SEM_ID_CAP_SHRINK = "https://arena2036.de/vws4ls/capability/1/0/ShrinkCapability";
    public const string SEM_ID_CAP_SPOTTAPE = "https://arena2036.de/vws4ls/capability/1/0/SpotTapeCapability";
    public const string SEM_ID_CAP_TAPE = "https://arena2036.de/vws4ls/capability/1/0/TapeCapability";
    public const string SEM_ID_CAP_TUBE = "https://arena2036.de/vws4ls/capability/1/0/TubeCapability";
    public const string SEM_ID_CAP_WIRETWIST = "https://arena2036.de/vws4ls/capability/1/0/WireTwistCapability";
    public const string SEM_ID_CAP_SIMPLETWIST = "https://arena2036.de/vws4ls/capability/1/0/SimpleTwistCapability";
    public const string SEM_ID_CAP_ROUTE = "https://arena2036.de/vws4ls/capability/1/0/RouteCapability";
    public const string SEM_ID_CAP_FUSEBOXASSEMBLY = "https://arena2036.de/vws4ls/capability/1/0/FuseBoxAssemblyCapability";
    public const string SEM_ID_CAP_FOAM = "https://arena2036.de/vws4ls/capability/1/0/FoamCapability";
    public const string SEM_ID_CAP_SCREW = "https://arena2036.de/vws4ls/capability/1/0/ScrewCapability";
    public const string SEM_ID_CAP_SCAN = "https://arena2036.de/vws4ls/capability/1/0/ScanCapability";
    public const string SEM_ID_CAP_TEST = "https://arena2036.de/vws4ls/capability/1/0/TestCapability";


    public static Dictionary<string, List<Property>> ConstraintsByCapability = new Dictionary<string, List<Property>>()
    {
        {
            SEM_ID_CAP_CUT, new List<Property>()
            {
                new Property() {Name = "NominalLength", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "LengthUpperLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "LengthLowerLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range}
            }
        },
        {
            SEM_ID_CAP_CUTWIRE, new List<Property>()
            {
                new Property() {Name = "NominalLength", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "LengthUpperLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "LengthLowerLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "WireType", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List},
                new Property() {Name = "WireCrossSectionArea", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range}
            }
        },
        {
            SEM_ID_CAP_CUTTUBE, new List<Property>()
            {
                new Property() {Name = "NominalLength", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "LengthUpperLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "LengthLowerLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "TubeType", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List},
                new Property() {Name = "TubeDiameter", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range}
            }
        },
        {
            SEM_ID_CAP_STRIP, new List<Property>()
            {
                new Property() {Name = "NominalStrippingLength", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "StrippingLengthUpperLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "StrippingLengthLowerLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "CenterStripping", ValueType = DataTypeDefXsd.Boolean, ConstraintType = ConstraintType.FixedValue},
                new Property() {Name = "Layer", ValueType = DataTypeDefXsd.Boolean, ConstraintType = ConstraintType.FixedValue},
                new Property() {Name = "IncisionMonitoring", ValueType = DataTypeDefXsd.Boolean, ConstraintType = ConstraintType.FixedValue},
                new Property() {Name = "WireEnd", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List}
            }
        },
        {
            SEM_ID_CAP_SLIT, new List<Property>()
            {
                new Property() {Name = "NominalSlittingLength", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "Layer", ValueType = DataTypeDefXsd.Boolean, ConstraintType = ConstraintType.FixedValue},
                new Property() {Name = "WireEnd", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List}
            }
        },
        {
            SEM_ID_CAP_CRIMP, new List<Property>()
            {
                new Property() {Name = "WireType", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List},
                new Property() {Name = "WireCrossSectionArea", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "TerminalPartNumber", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List},
                new Property() {Name = "CrimpForceMonitoring", ValueType = DataTypeDefXsd.Boolean, ConstraintType = ConstraintType.FixedValue},
                new Property() {Name = "CrimpHeightUpperLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "CrimpHeightLowerLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "CrimpWidthUpperLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "CrimpWidthLowerLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range}
            }
        },
        {
            SEM_ID_CAP_MARKWIRE, new List<Property>()
            {
                new Property() {Name = "MarkingType", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List},
                new Property() {Name = "ContentType", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List},
                new Property() {Name = "CharHeight", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "Color", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List}
            }
        },
        {
            SEM_ID_CAP_TINNING, new List<Property>()
            {
                new Property() {Name = "TemperatureAccuracyUpperLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "TemperatureAccuracyLowerLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range}
            }
        },
        {
            SEM_ID_CAP_REFINEMENTOFCABLELUGS, new List<Property>()
            {
                new Property() {Name = "SolderDurationUpperLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "SolderDurationLowerLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range}
            }
        },
        {
            SEM_ID_CAP_SEAL, new List<Property>()
            {
                new Property() {Name = "SealPartNumber", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List},
                new Property() {Name = "SealPositionUpperLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "SealPositionLowerLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range}
            }
        },
        {
            SEM_ID_CAP_SLEEVE, new List<Property>()
            {
                new Property() {Name = "SleevePartNumber", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List},
                new Property() {Name = "PullOutCheck", ValueType = DataTypeDefXsd.Boolean, ConstraintType = ConstraintType.FixedValue}
            }
        },
        {
            SEM_ID_CAP_STRANDTWIST, new List<Property>()
            {
                new Property() {Name = "WireCrossSectionArea", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range}
            }
        },
        {
            SEM_ID_CAP_SHIELDTWIST, new List<Property>()
            {
            }
        },
        {
            SEM_ID_CAP_BLOCKLOAD, new List<Property>()
            {

            }
        },
        {
            SEM_ID_CAP_CONNECTORANDHOUSING, new List<Property>()
            {
                new Property() {Name = "ConnectorPartNumber", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List},
                new Property() {Name = "InsertionForceCurveMonitoring", ValueType = DataTypeDefXsd.Boolean, ConstraintType = ConstraintType.FixedValue}
            }
        },
        {
            SEM_ID_CAP_ULTRASONICWELD, new List<Property>()
            {
                new Property() {Name = "SumCrossSectionArea", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range}
            }
        },
        {
            SEM_ID_CAP_TERMINAL, new List<Property>()
            {
                new Property() {Name = "TerminalPartNumber", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List}
            }
        },
        {
            SEM_ID_CAP_COVER, new List<Property>()
            {
                new Property() {Name = "BundleDiameter", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "StartNode", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List},
                new Property() {Name = "EndNode", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List}
            }
        },
        {
            SEM_ID_CAP_SHRINK, new List<Property>()
            {
                new Property() {Name = "NominalTemperature", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range}
            }
        },
        {
            SEM_ID_CAP_SPOTTAPE, new List<Property>()
            {

            }
        },
        {
            SEM_ID_CAP_TAPE, new List<Property>()
            {
                new Property() {Name = "TapingMethod", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List}
            }
        },
        {
            SEM_ID_CAP_TUBE, new List<Property>()
            {
                new Property() {Name = "TubeType", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List}
            }
        },
        {
            SEM_ID_CAP_WIRETWIST, new List<Property>()
            {
                new Property() {Name = "NominalWireEndLength", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "NominalTensionForce", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "NominalOpenEndSide1", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "NominalTrimmedOpenEndLengthSide1", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "SpotTapeSide1", ValueType = DataTypeDefXsd.Boolean, ConstraintType = ConstraintType.FixedValue},
                new Property() {Name = "NominalOpenEndSide2", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "NominalTrimmedOpenEndLengthSide2", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "SpotTapeSide2", ValueType = DataTypeDefXsd.Boolean, ConstraintType = ConstraintType.FixedValue},
                new Property() {Name = "WireEndLengthUpperLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "WireEndLengthLowerLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "LayLengthUpperLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "LayLengthLowerLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "OpenEndLowerLimitSide1", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "OpenEndUpperLimitSide1", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "OpenEndLowerLimitSide2", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "OpenEndUpperLimitSide2", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range}
            }
        },
        {
            SEM_ID_CAP_SIMPLETWIST, new List<Property>()
            {
                new Property() {Name = "NominalElasticity", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range}
            }
        },
        {
            SEM_ID_CAP_ROUTE, new List<Property>()
            {

            }
        },
        {
            SEM_ID_CAP_FUSEBOXASSEMBLY, new List<Property>()
            {
                new Property() {Name = "FuseBoxPartNumber", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List},
                new Property() {Name = "MountForceAccuracyLowerLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "MountForceAccuracyUpperLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range}
            }
        },
        {
            SEM_ID_CAP_FOAM, new List<Property>()
            {
                new Property() {Name = "FoamObjectPartNumbers", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List}
            }
        },
        {
            SEM_ID_CAP_SCREW, new List<Property>()
            {
                new Property() {Name = "FuseBoxPartNumber", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List},
                new Property() {Name = "PositionName", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List},
                new Property() {Name = "WrenchSize", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "NominalTorque", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "TighteningCurve", ValueType = DataTypeDefXsd.Boolean, ConstraintType = ConstraintType.FixedValue},
                new Property() {Name = "TorqueLowerLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "TorqueUpperLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "AngleLowerLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range},
                new Property() {Name = "AngleUpperLimit", ValueType = DataTypeDefXsd.Double, ConstraintType = ConstraintType.Range}
            }
        },
        {
            SEM_ID_CAP_SCAN, new List<Property>()
            {
                new Property() {Name = "ComponentPartNumber", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List},
                new Property() {Name = "BarcodePosition", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List},
                new Property() {Name = "BarcodeType", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List},
                new Property() {Name = "Grading", ValueType = DataTypeDefXsd.Boolean, ConstraintType = ConstraintType.FixedValue}
            }
        },
        {
            SEM_ID_CAP_TEST, new List<Property>()
            {
                new Property() {Name = "TestSpecificationId", ValueType = DataTypeDefXsd.String, ConstraintType = ConstraintType.List}
            }
        }
    };


    public class Property
    {
        public string Name { get; set; }
        public DataTypeDefXsd ValueType { get; set; }
        public ConstraintType ConstraintType { get; set; }
    }

    public enum ConstraintType
    {
        FixedValue = 0,
        Range = 1,
        List = 2
    }
}
