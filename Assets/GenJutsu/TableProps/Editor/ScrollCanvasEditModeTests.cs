using NUnit.Framework;
using UnityEngine;

namespace GenJutsu.TableProps.EditorTools
{
    public class ScrollCanvasEditModeTests
    {
        [Test]
        public void Awake_StartsClean_WithoutInkStrokes()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Paper";
            try
            {
                var canvas = go.AddComponent<ScrollCanvas>();
                Assert.AreEqual(0, canvas.CountDarkInkPixels());
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

    }
}
