using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Xeon.UniTerminal.Assets;

namespace Xeon.UniTerminal.Tests
{
    /// <summary>
    /// ValueConverterのアセット参照変換機能のテスト。
    /// </summary>
    public class ValueConverterAssetTests
    {
        private List<UnityEngine.Object> createdAssets;

        [SetUp]
        public void SetUp()
        {
            createdAssets = new List<UnityEngine.Object>();
            AssetManager.ResetInstance();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in createdAssets)
            {
                if (asset != null)
                    UnityEngine.Object.DestroyImmediate(asset);
            }
            createdAssets.Clear();
            AssetManager.Instance.Registry.Clear();
            AssetManager.ResetInstance();
        }

        private T CreateAsset<T>(string name) where T : UnityEngine.Object
        {
            T asset;

            if (typeof(T) == typeof(Material))
            {
                asset = new Material(Shader.Find("Standard")) as T;
            }
            else if (typeof(T) == typeof(Texture2D))
            {
                asset = new Texture2D(4, 4) as T;
            }
            else if (typeof(T) == typeof(Mesh))
            {
                asset = new Mesh() as T;
            }
            else if (typeof(T) == typeof(GameObject))
            {
                asset = new GameObject(name) as T;
            }
            else
            {
                throw new NotSupportedException($"Cannot create asset of type {typeof(T).Name}");
            }

            if (asset != null)
            {
                asset.name = name;
                createdAssets.Add(asset);
            }

            return asset;
        }

        // VALCNV-AST-001 Texture2D null/none
        [Test]
        public void Convert_TextureNull_ReturnsNull()
        {
            var result = ValueConverter.Convert("null", typeof(Texture2D));
            Assert.IsNull(result);
        }

        // VALCNV-AST-002 Texture2D none
        [Test]
        public void Convert_TextureNone_ReturnsNull()
        {
            var result = ValueConverter.Convert("none", typeof(Texture2D));
            Assert.IsNull(result);
        }

        // VALCNV-AST-003 Texture2D インスタンスID参照
        [Test]
        public void Convert_TextureByInstanceId_ReturnsAsset()
        {
            var texture = CreateAsset<Texture2D>("TestTexture");
            AssetManager.Instance.Registry.Register(texture, "test/path", "TestProvider");

            var specifier = $"#{texture.GetInstanceID()}";
            var result = ValueConverter.Convert(specifier, typeof(Texture2D));

            Assert.IsNotNull(result);
            Assert.AreEqual(texture, result);
        }

        // VALCNV-AST-004 Texture2D 名前参照
        [Test]
        public void Convert_TextureByName_ReturnsAsset()
        {
            var texture = CreateAsset<Texture2D>("UniqueTexture");
            AssetManager.Instance.Registry.Register(texture, "test/path", "TestProvider");

            var result = ValueConverter.Convert("UniqueTexture", typeof(Texture2D));

            Assert.IsNotNull(result);
            Assert.AreEqual(texture, result);
        }

        // VALCNV-AST-005 存在しないTexture2D
        [Test]
        public void Convert_TextureNotFound_ThrowsException()
        {
            Assert.Throws<FormatException>(() =>
                ValueConverter.Convert("NonExistentTexture12345", typeof(Texture2D)));
        }

        // VALCNV-AST-010 Mesh null/none
        [Test]
        public void Convert_MeshNull_ReturnsNull()
        {
            var result = ValueConverter.Convert("null", typeof(Mesh));
            Assert.IsNull(result);
        }

        // VALCNV-AST-011 Mesh インスタンスID参照
        [Test]
        public void Convert_MeshByInstanceId_ReturnsAsset()
        {
            var mesh = CreateAsset<Mesh>("TestMesh");
            AssetManager.Instance.Registry.Register(mesh, "test/path", "TestProvider");

            var specifier = $"#{mesh.GetInstanceID()}";
            var result = ValueConverter.Convert(specifier, typeof(Mesh));

            Assert.IsNotNull(result);
            Assert.AreEqual(mesh, result);
        }

        // VALCNV-AST-012 Mesh 名前参照
        [Test]
        public void Convert_MeshByName_ReturnsAsset()
        {
            var mesh = CreateAsset<Mesh>("UniqueMesh");
            AssetManager.Instance.Registry.Register(mesh, "test/path", "TestProvider");

            var result = ValueConverter.Convert("UniqueMesh", typeof(Mesh));

            Assert.IsNotNull(result);
            Assert.AreEqual(mesh, result);
        }

        // VALCNV-AST-020 Material null/none
        [Test]
        public void Convert_MaterialNull_ReturnsNull()
        {
            var result = ValueConverter.Convert("null", typeof(Material));
            Assert.IsNull(result);
        }

        // VALCNV-AST-030 Shader null/none
        [Test]
        public void Convert_ShaderNull_ReturnsNull()
        {
            var result = ValueConverter.Convert("null", typeof(Shader));
            Assert.IsNull(result);
        }

        // VALCNV-AST-031 Shader.Find検索
        [Test]
        public void Convert_ShaderByName_ReturnsShader()
        {
            var result = ValueConverter.Convert("Standard", typeof(Shader));

            Assert.IsNotNull(result);
            Assert.IsInstanceOf<Shader>(result);
        }

        // VALCNV-AST-032 存在しないShader
        [Test]
        public void Convert_ShaderNotFound_ThrowsException()
        {
            Assert.Throws<FormatException>(() =>
                ValueConverter.Convert("NonExistentShader12345", typeof(Shader)));
        }

        // VALCNV-AST-040 GameObject インスタンスID参照
        [Test]
        public void Convert_GameObjectByInstanceId_ReturnsObject()
        {
            var go = CreateAsset<GameObject>("TestGO");

            var specifier = $"#{go.GetInstanceID()}";
            var result = ValueConverter.Convert(specifier, typeof(GameObject));

            Assert.IsNotNull(result);
            Assert.AreEqual(go, result);
        }

        // VALCNV-AST-041 GameObject null/none
        [Test]
        public void Convert_GameObjectNull_ReturnsNull()
        {
            var result = ValueConverter.Convert("null", typeof(GameObject));
            Assert.IsNull(result);
        }

        // VALCNV-AST-050 Component インスタンスID参照
        [Test]
        public void Convert_ComponentByInstanceId_ReturnsComponent()
        {
            var go = CreateAsset<GameObject>("TestGO");
            var rb = go.AddComponent<Rigidbody>();

            var specifier = $"#{rb.GetInstanceID()}";
            var result = ValueConverter.Convert(specifier, typeof(Rigidbody));

            Assert.IsNotNull(result);
            Assert.AreEqual(rb, result);
        }

        // VALCNV-AST-051 Component null/none
        [Test]
        public void Convert_ComponentNull_ReturnsNull()
        {
            var result = ValueConverter.Convert("null", typeof(Transform));
            Assert.IsNull(result);
        }

        // VALCNV-AST-060 同名アセット複数時のエラー
        [Test]
        public void Convert_MultipleAssetsWithSameName_ThrowsException()
        {
            var texture1 = CreateAsset<Texture2D>("SameName");
            var texture2 = CreateAsset<Texture2D>("SameName");

            AssetManager.Instance.Registry.Register(texture1, "path1", "TestProvider");
            AssetManager.Instance.Registry.Register(texture2, "path2", "TestProvider");

            Assert.Throws<FormatException>(() =>
                ValueConverter.Convert("SameName", typeof(Texture2D)));
        }

        // VALCNV-AST-061 同名だが型が異なる場合の正しい解決
        [Test]
        public void Convert_SameNameDifferentTypes_ResolvesCorrectly()
        {
            var texture = CreateAsset<Texture2D>("SharedName");
            var mesh = CreateAsset<Mesh>("SharedName");

            AssetManager.Instance.Registry.Register(texture, "path1", "TestProvider");
            AssetManager.Instance.Registry.Register(mesh, "path2", "TestProvider");

            // 型指定で正しいアセットが解決されるべき
            var textureResult = ValueConverter.Convert("SharedName", typeof(Texture2D));
            Assert.IsNotNull(textureResult);
            Assert.AreEqual(texture, textureResult);

            var meshResult = ValueConverter.Convert("SharedName", typeof(Mesh));
            Assert.IsNotNull(meshResult);
            Assert.AreEqual(mesh, meshResult);
        }

        // VALCNV-AST-070 TryConvert成功
        [Test]
        public void TryConvert_ValidAsset_ReturnsTrue()
        {
            var texture = CreateAsset<Texture2D>("TestTexture");
            AssetManager.Instance.Registry.Register(texture, "test/path", "TestProvider");

            var success = ValueConverter.TryConvert("TestTexture", typeof(Texture2D), out var result);

            Assert.IsTrue(success);
            Assert.AreEqual(texture, result);
        }

        // VALCNV-AST-071 TryConvert失敗
        [Test]
        public void TryConvert_InvalidAsset_ReturnsFalse()
        {
            var success = ValueConverter.TryConvert("NonExistent12345", typeof(Texture2D), out var result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        // VALCNV-AST-080 Format Material
        [Test]
        public void Format_Material_ReturnsQuotedName()
        {
            var material = CreateAsset<Material>("TestMaterial");

            var result = ValueConverter.Format(material);

            Assert.IsTrue(result.Contains("TestMaterial"));
        }

        // VALCNV-AST-081 Format Texture
        [Test]
        public void Format_Texture_ReturnsQuotedName()
        {
            var texture = CreateAsset<Texture2D>("TestTexture");

            var result = ValueConverter.Format(texture);

            Assert.IsTrue(result.Contains("TestTexture"));
        }

        // VALCNV-AST-082 Format null
        [Test]
        public void Format_NullAsset_ReturnsNullString()
        {
            Material material = null;

            var result = ValueConverter.Format(material);

            Assert.AreEqual("(null)", result);
        }

        // VALCNV-AST-090 空文字列入力
        [Test]
        public void Convert_EmptyString_ReturnsNull()
        {
            var result = ValueConverter.Convert("", typeof(Texture2D));
            Assert.IsNull(result);
        }

        // VALCNV-AST-091 NULL指定（大文字）
        [Test]
        public void Convert_NullUppercase_ReturnsNull()
        {
            var result = ValueConverter.Convert("NULL", typeof(Texture2D));
            Assert.IsNull(result);
        }

        // VALCNV-AST-092 None指定（大文字小文字混在）
        [Test]
        public void Convert_NoneMixedCase_ReturnsNull()
        {
            var result = ValueConverter.Convert("NoNe", typeof(Mesh));
            Assert.IsNull(result);
        }

        // VALCNV-AST-100 不正なインスタンスID形式
        [Test]
        public void Convert_InvalidInstanceIdFormat_ThrowsException()
        {
            Assert.Throws<FormatException>(() =>
                ValueConverter.Convert("#999999999", typeof(Texture2D)));
        }
    }
}
