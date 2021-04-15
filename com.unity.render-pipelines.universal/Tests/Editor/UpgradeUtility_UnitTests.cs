using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using IMaterial = UnityEditor.Rendering.UpgradeUtility.IMaterial;
using RenameType = UnityEditor.Rendering.MaterialUpgrader.RenameType;
using static UnityEditor.Rendering.Tests.UpgraderTestUtility;

namespace UnityEditor.Rendering.Tests
{
    /// <summary>
    /// Utility to generate arguments for <see cref="UpgradeUtility"/> using mock objects for parameterized tests.
    /// </summary>
    static class UpgraderTestUtility
    {
        internal static Dictionary<string, IReadOnlyList<MaterialUpgrader>> CreateUpgradePathsToNewShaders(
            (string OldShader, string NewShader, (string From, string To, int Type)[] Renames)[] materialUpgraderParams
        )
        {
            var result = new Dictionary<string, List<MaterialUpgrader>>();
            foreach (var upgrader in CreateMaterialUpgraders(materialUpgraderParams))
            {
                if (!result.TryGetValue(upgrader.NewShader, out var upgraders))
                    upgraders = result[upgrader.NewShader] = new List<MaterialUpgrader>();
                upgraders.Add(upgrader);
            }
            return result.ToDictionary(kv => kv.Key, kv => kv.Value as IReadOnlyList<MaterialUpgrader>);
        }

        internal static IReadOnlyList<MaterialUpgrader> CreateMaterialUpgraders(
            params (string OldShader, string NewShader, (string From, string To, int Type)[] Renames)[] materialUpgraderParams
        )
        {
            var result = new List<MaterialUpgrader>(materialUpgraderParams.Length);

            foreach (var muParams in materialUpgraderParams)
            {
                var materialUpgrader = new MaterialUpgrader();
                materialUpgrader.RenameShader(muParams.OldShader, muParams.NewShader);
                foreach (var rename in muParams.Renames)
                {
                    switch ((RenameType)rename.Type)
                    {
                        case RenameType.Color:
                            materialUpgrader.RenameColor(rename.From, rename.To);
                            break;
                        case RenameType.Float:
                            materialUpgrader.RenameFloat(rename.From, rename.To);
                            break;
                    }
                }

                result.Add(materialUpgrader);
            }

            return result;
        }
    }

    class UpgradeUtility_UnitTests
    {
        [Test]
        public void GetAllUpgradePathsToShaders_WhenUpgraderNotInitialized_DoesNotThrow()
        {
            var upgraders = new[] { new MaterialUpgrader() };

            Assert.DoesNotThrow(() => UpgradeUtility.GetAllUpgradePathsToShaders(upgraders));
        }

        [Test]
        public void GetAllUpgradePathsToShaders_ProducesExpectedMappings()
        {
            var aToA = new MaterialUpgrader(); aToA.RenameShader("oldA", "newA");
            var bToA = new MaterialUpgrader(); bToA.RenameShader("oldB", "newA");
            var xToX = new MaterialUpgrader(); xToX.RenameShader("oldX", "newX");
            var upgraders = new[] { aToA, bToA, xToX };

            var upgradePaths = UpgradeUtility.GetAllUpgradePathsToShaders(upgraders);

            var actualPairs = upgradePaths.SelectMany(kv => kv.Value.Select(v => (kv.Key, v)));
            var expectedPairs = new[]
            {
                ("newA", aToA),
                ("newA", bToA),
                ("newX", xToX)
            };
            Assert.That(actualPairs, Is.EquivalentTo(expectedPairs));
        }

        static readonly TestCaseData[] k_UnknownUpgradePathTestCases =
        {
            new TestCaseData(
                    "_Color", "NewShader", RenameType.Color,
                    new[]
                    {
                        ("OldShader", "NewShader", new[] { (From: "_Color", To: "_BaseColor", Type: (int)RenameType.Color) })
                    },
                    "_BaseColor"
                )
                .Returns(SerializedShaderPropertyUsage.UsedByUpgraded)
                .SetName("Upgraded color property"),
            new TestCaseData(
                    "_MainTex_ST", "NewShader", RenameType.Float,
                    new[]
                    {
                        ("OldShader", "NewShader", new[] { (From: "_MainTex_ST", To: "_BaseMap_ST_ST", Type: (int)RenameType.Float) })
                    },
                    "_BaseMap_ST_ST"
                )
                .Returns(SerializedShaderPropertyUsage.UsedByUpgraded)
                .SetName("Single target material, upgraded, float property"),
            new TestCaseData(
                    "_Color", "NewShader", RenameType.Color,
                    new[]
                    {
                        ("OldShader1", "NewShader", new[] { (From: "_Color", To: "_BaseColor1", Type: (int)RenameType.Color) }),
                        ("OldShader2", "NewShader", new[] { (From: "_Color", To: "_BaseColor2", Type: (int)RenameType.Color) })
                    },
                    "_BaseColor1"
                )
                .Returns(SerializedShaderPropertyUsage.UsedByAmbiguouslyUpgraded)
                .SetName("Single target material, upgraded with multiple paths, color property"),
            new TestCaseData(
                    "_MainTex_ST", "NewShader", RenameType.Float,
                    new[]
                    {
                        ("OldShader1", "NewShader", new[] { (From: "_MainTex_ST", To: "_BaseMap_ST_ST1", Type: (int)RenameType.Float) }),
                        ("OldShader2", "NewShader", new[] { (From: "_MainTex_ST", To: "_BaseMap_ST_ST2", Type: (int)RenameType.Float) })
                    },
                    "_BaseMap_ST_ST1"
                )
                .Returns(SerializedShaderPropertyUsage.UsedByAmbiguouslyUpgraded)
                .SetName("Single target material, upgraded with multiple paths, float property"),
            new TestCaseData(
                    "_Color", "OldShader", RenameType.Color,
                    new[]
                    {
                        ("OldShader", "NewShader", new[] { (From: "_Color", To: "_BaseColor", Type: (int)RenameType.Color) })
                    },
                    "_Color"
                )
                .Returns(SerializedShaderPropertyUsage.UsedByNonUpgraded)
                .SetName("Single target material, not upgraded, color property"),
            new TestCaseData(
                    "_MainTex_ST", "OldShader", RenameType.Float,
                    new[]
                    {
                        ("OldShader", "NewShader", new[] { (From: "_MainTex_ST", To: "_BaseMap_ST_ST", Type: (int)RenameType.Float) })
                    },
                    "_MainTex_ST"
                )
                .Returns(SerializedShaderPropertyUsage.UsedByNonUpgraded)
                .SetName("Single target material, not upgraded, float property")
        };

        [TestCaseSource(nameof(k_UnknownUpgradePathTestCases))]
        public SerializedShaderPropertyUsage GetNewPropertyName_WhenUpgradePathIsUnknown_ReturnsExpectedResult(
            string propertyName, string materialShaderName, RenameType renameType,
            (string OldShader, string NewShader, (string From, string To, int Type)[] Renames)[] materialUpgraders,
            string expectedNewName
        )
        {
            var material = new Mock<IMaterial>();
            material.SetupGet(m => m.ShaderName).Returns(materialShaderName);
            var allUpgradePathsToNewShaders = CreateUpgradePathsToNewShaders(materialUpgraders);

            var actualUsage = UpgradeUtility.GetNewPropertyName(
                propertyName,
                material.Object,
                renameType,
                allUpgradePathsToNewShaders,
                upgradePathsUsedByMaterials: default,
                out var actualNewPropertyName
            );

            Assert.That(actualNewPropertyName, Is.EqualTo(expectedNewName));
            return actualUsage;
        }

        static readonly TestCaseData[] k_KnownUpgradePathTestCases =
        {
            new TestCaseData(
                    "_Color", "NewShader", RenameType.Color,
                    new[]
                    {
                        ("OldShader", "NewShader", new[] { (From: "_Color", To: "_BaseColor", Type: (int)RenameType.Color) })
                    },
                    "_BaseColor"
                )
                .Returns(SerializedShaderPropertyUsage.UsedByUpgraded)
                .SetName("Single target material, color property"),
            new TestCaseData(
                    "_MainTex_ST", "NewShader", RenameType.Float,
                    new[]
                    {
                        ("OldShader", "NewShader", new[] { (From: "_MainTex_ST", To: "_BaseMap_ST", Type: (int)RenameType.Float) })
                    },
                    "_BaseMap_ST"
                )
                .Returns(SerializedShaderPropertyUsage.UsedByUpgraded)
                .SetName("Single target material, float property")
        };

        [TestCaseSource(nameof(k_KnownUpgradePathTestCases))]
        public SerializedShaderPropertyUsage GetNewPropertyName_WhenUpgradePathIsKnown_UsesKnownUpgradePath(
            string propertyName, string materialShaderName, RenameType renameType,
            (string OldShader, string NewShader, (string From, string To, int Type)[] Renames)[] materialUpgraders,
            string expectedNewName
        )
        {
            var material = new Mock<IMaterial>();
            material.SetupGet(m => m.ShaderName).Returns(materialShaderName);
            var allUpgradePathsToNewShaders = CreateUpgradePathsToNewShaders(materialUpgraders);
            var upgradePathsUsedByMaterials = new Dictionary<IMaterial, MaterialUpgrader>
            {
                [material.Object] = CreateMaterialUpgraders(materialUpgraders)[0]
            };

            var actualUsage = UpgradeUtility.GetNewPropertyName(
                propertyName,
                material.Object,
                renameType,
                allUpgradePathsToNewShaders,
                upgradePathsUsedByMaterials,
                out var actualNewPropertyName
            );

            Assert.That(actualNewPropertyName, Is.EqualTo(expectedNewName));
            return actualUsage;
        }
    }
}
