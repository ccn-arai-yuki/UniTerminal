using NUnit.Framework;
using UnityEngine;

namespace Xeon.UniTerminal.Tests
{
    /// <summary>
    /// TypeResolverのアセット型解決機能のテスト。
    /// </summary>
    public class TypeResolverAssetTests
    {
        // TYPR-AST-001 Texture2D解決
        [Test]
        public void ResolveAssetType_Texture2D_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("Texture2D");
            Assert.AreEqual(typeof(Texture2D), type);
        }

        // TYPR-AST-002 texture2dエイリアス（小文字）
        [Test]
        public void ResolveAssetType_Texture2DLowercase_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("texture2d");
            Assert.AreEqual(typeof(Texture2D), type);
        }

        // TYPR-AST-003 Textureエイリアス
        [Test]
        public void ResolveAssetType_Texture_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("Texture");
            Assert.AreEqual(typeof(Texture), type);
        }

        // TYPR-AST-010 Sprite解決
        [Test]
        public void ResolveAssetType_Sprite_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("Sprite");
            Assert.AreEqual(typeof(Sprite), type);
        }

        // TYPR-AST-011 spriteエイリアス（小文字）
        [Test]
        public void ResolveAssetType_SpriteLowercase_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("sprite");
            Assert.AreEqual(typeof(Sprite), type);
        }

        // TYPR-AST-020 Material解決
        [Test]
        public void ResolveAssetType_Material_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("Material");
            Assert.AreEqual(typeof(Material), type);
        }

        // TYPR-AST-021 materialエイリアス（小文字）
        [Test]
        public void ResolveAssetType_MaterialLowercase_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("material");
            Assert.AreEqual(typeof(Material), type);
        }

        // TYPR-AST-030 Mesh解決
        [Test]
        public void ResolveAssetType_Mesh_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("Mesh");
            Assert.AreEqual(typeof(Mesh), type);
        }

        // TYPR-AST-031 meshエイリアス（小文字）
        [Test]
        public void ResolveAssetType_MeshLowercase_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("mesh");
            Assert.AreEqual(typeof(Mesh), type);
        }

        // TYPR-AST-040 AudioClip解決
        [Test]
        public void ResolveAssetType_AudioClip_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("AudioClip");
            Assert.AreEqual(typeof(AudioClip), type);
        }

        // TYPR-AST-041 audioエイリアス
        [Test]
        public void ResolveAssetType_Audio_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("audio");
            Assert.AreEqual(typeof(AudioClip), type);
        }

        // TYPR-AST-042 audioclipエイリアス（小文字）
        [Test]
        public void ResolveAssetType_AudioClipLowercase_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("audioclip");
            Assert.AreEqual(typeof(AudioClip), type);
        }

        // TYPR-AST-050 Shader解決
        [Test]
        public void ResolveAssetType_Shader_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("Shader");
            Assert.AreEqual(typeof(Shader), type);
        }

        // TYPR-AST-051 shaderエイリアス（小文字）
        [Test]
        public void ResolveAssetType_ShaderLowercase_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("shader");
            Assert.AreEqual(typeof(Shader), type);
        }

        // TYPR-AST-060 Font解決
        [Test]
        public void ResolveAssetType_Font_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("Font");
            Assert.AreEqual(typeof(Font), type);
        }

        // TYPR-AST-061 fontエイリアス（小文字）
        [Test]
        public void ResolveAssetType_FontLowercase_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("font");
            Assert.AreEqual(typeof(Font), type);
        }

        // TYPR-AST-070 GameObjectエイリアス
        [Test]
        public void ResolveAssetType_GameObject_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("GameObject");
            Assert.AreEqual(typeof(GameObject), type);
        }

        // TYPR-AST-071 prefabエイリアス
        [Test]
        public void ResolveAssetType_Prefab_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("prefab");
            Assert.AreEqual(typeof(GameObject), type);
        }

        // TYPR-AST-072 gameobjectエイリアス（小文字）
        [Test]
        public void ResolveAssetType_GameObjectLowercase_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("gameobject");
            Assert.AreEqual(typeof(GameObject), type);
        }

        // TYPR-AST-080 AnimationClip解決
        [Test]
        public void ResolveAssetType_AnimationClip_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("AnimationClip");
            Assert.AreEqual(typeof(AnimationClip), type);
        }

        // TYPR-AST-081 animationエイリアス
        [Test]
        public void ResolveAssetType_Animation_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("animation");
            Assert.AreEqual(typeof(AnimationClip), type);
        }

        // TYPR-AST-090 ScriptableObject解決
        [Test]
        public void ResolveAssetType_ScriptableObject_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("ScriptableObject");
            Assert.AreEqual(typeof(ScriptableObject), type);
        }

        // TYPR-AST-091 soエイリアス
        [Test]
        public void ResolveAssetType_SO_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("so");
            Assert.AreEqual(typeof(ScriptableObject), type);
        }

        // TYPR-AST-100 TextAsset解決
        [Test]
        public void ResolveAssetType_TextAsset_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("TextAsset");
            Assert.AreEqual(typeof(TextAsset), type);
        }

        // TYPR-AST-101 textエイリアス
        [Test]
        public void ResolveAssetType_Text_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("text");
            Assert.AreEqual(typeof(TextAsset), type);
        }

        // TYPR-AST-110 フルネーム解決
        [Test]
        public void ResolveAssetType_FullName_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("UnityEngine.Material");
            Assert.AreEqual(typeof(Material), type);
        }

        // TYPR-AST-111 存在しないフルネーム
        [Test]
        public void ResolveAssetType_NonExistentFullName_ReturnsNull()
        {
            var type = TypeResolver.ResolveAssetType("UnityEngine.NonExistent12345");
            Assert.IsNull(type);
        }

        // TYPR-AST-120 null入力
        [Test]
        public void ResolveAssetType_Null_ReturnsNull()
        {
            var type = TypeResolver.ResolveAssetType(null);
            Assert.IsNull(type);
        }

        // TYPR-AST-121 空文字列入力
        [Test]
        public void ResolveAssetType_Empty_ReturnsNull()
        {
            var type = TypeResolver.ResolveAssetType("");
            Assert.IsNull(type);
        }

        // TYPR-AST-122 存在しない型名
        [Test]
        public void ResolveAssetType_NonExistent_ReturnsNull()
        {
            var type = TypeResolver.ResolveAssetType("NonExistentType12345");
            Assert.IsNull(type);
        }

        // TYPR-AST-130 一般的なアセット型名リスト
        [Test]
        public void GetCommonAssetTypeNames_ReturnsExpectedTypes()
        {
            var names = TypeResolver.GetCommonAssetTypeNames();

            Assert.IsNotNull(names);
            Assert.IsTrue(names.Length > 0);
            Assert.Contains("Texture2D", names);
            Assert.Contains("Material", names);
            Assert.Contains("Mesh", names);
            Assert.Contains("AudioClip", names);
            Assert.Contains("Shader", names);
            Assert.Contains("Font", names);
            Assert.Contains("GameObject", names);
            Assert.Contains("AnimationClip", names);
            Assert.Contains("ScriptableObject", names);
            Assert.Contains("TextAsset", names);
        }

        // TYPR-AST-140 大文字小文字混在
        [Test]
        public void ResolveAssetType_MixedCase_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("TEXTURE2D");
            Assert.AreEqual(typeof(Texture2D), type);
        }

        // TYPR-AST-141 部分大文字
        [Test]
        public void ResolveAssetType_MixedCase2_ReturnsCorrectType()
        {
            var type = TypeResolver.ResolveAssetType("AudioCLIP");
            Assert.AreEqual(typeof(AudioClip), type);
        }
    }
}
