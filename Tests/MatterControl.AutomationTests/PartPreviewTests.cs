﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MatterHackers.Agg;
using MatterHackers.DataConverters3D;
using MatterHackers.GuiAutomation;
using MatterHackers.MatterControl.PartPreviewWindow;
using MatterHackers.MatterControl.PrintQueue;
using MatterHackers.MeshVisualizer;
using MatterHackers.PolygonMesh;
using MatterHackers.VectorMath;
using Newtonsoft.Json;
using NUnit.Framework;

namespace MatterHackers.MatterControl.Tests.Automation
{
	[TestFixture, Category("MatterControl.UI.Automation"), RunInApplicationDomain, Apartment(ApartmentState.STA)]
	public class PartPreviewTests
	{
		[Test]
		public async Task CopyButtonMakesCopyOfPart()
		{
			await MatterControlUtilities.RunTest((testRunner) =>
			{
				testRunner.CloseSignInAndPrinterSelect();

				testRunner.AddItemToBedplate();

				// Get View3DWidget
				View3DWidget view3D = testRunner.GetWidgetByName("View3DWidget", out _, 3) as View3DWidget;
				var scene = view3D.InteractionLayer.Scene;

				testRunner.WaitForName("Calibration - Box.stl");
				Assert.AreEqual(1, scene.Children.Count, "Should have 1 part before copy");

				// Select scene object
				testRunner.Select3DPart("Calibration - Box.stl");

				// Click Copy button and count Scene.Children 
				testRunner.ClickByName("3D View Copy");
				testRunner.Delay(() => scene.Children.Count == 2, 3);
				Assert.AreEqual(2, scene.Children.Count, "Should have 2 parts after copy");

				// Click Copy button a second time and count Scene.Children
				testRunner.ClickByName("3D View Copy");
				testRunner.Delay(() => scene.Children.Count > 2, 3);
				Assert.AreEqual(3, scene.Children.Count, "Should have 3 parts after 2nd copy");

				return Task.CompletedTask;
			}, overrideWidth: 1300, maxTimeToRun: 60);
		}

		[Test]
		public async Task GroupAndUngroup()
		{
			await MatterControlUtilities.RunTest((testRunner) =>
			{
				testRunner.CloseSignInAndPrinterSelect();

				testRunner.AddItemToBedplate();

				// Get View3DWidget and count Scene.Children before Copy button is clicked
				View3DWidget view3D = testRunner.GetWidgetByName("View3DWidget", out _, 3) as View3DWidget;
				var scene = view3D.InteractionLayer.Scene;

				// Assert expected start count
				Assert.AreEqual(1, scene.Children.Count, "Should have one part before copy");

				// Select scene object
				testRunner.Select3DPart("Calibration - Box.stl");

				for (int i = 2; i <= 6; i++)
				{
					testRunner.ClickByName("3D View Copy");
					testRunner.Delay(() => scene.Children.Count == i, 3);
					Assert.AreEqual(i, scene.Children.Count, $"Should have {i} parts after copy");
				}

				// Get MeshGroupCount before Group is clicked
				Assert.AreEqual(6, scene.Children.Count, "Scene should have 6 parts after copy loop");

				// select all
				testRunner.Type("^a");

				testRunner.ClickByName("3D View Group");
				testRunner.Delay(() => scene.Children.Count == 1, 3);
				Assert.AreEqual(1, scene.Children.Count, $"Should have 1 parts after group");

				testRunner.ClickByName("3D View Ungroup");
				testRunner.Delay(() => scene.Children.Count == 6, 3);
				Assert.AreEqual(6, scene.Children.Count, $"Should have 6 parts after ungroup");

				return Task.CompletedTask;
			}, overrideWidth: 1300);
		}

		[Test]
		public async Task RemoveButtonRemovesParts()
		{
			await MatterControlUtilities.RunTest((testRunner) =>
			{
				testRunner.CloseSignInAndPrinterSelect();

				testRunner.AddItemToBedplate();

				var view3D = testRunner.GetWidgetByName("View3DWidget", out _) as View3DWidget;
				var scene = view3D.InteractionLayer.Scene;

				testRunner.Select3DPart("Calibration - Box.stl");

				Assert.AreEqual(1, scene.Children.Count, "There should be 1 part on the bed after AddDefaultFileToBedplate()");

				// Add 5 items
				for (int i = 0; i <= 4; i++)
				{
					testRunner.ClickByName("3D View Copy");
					testRunner.Delay(.5);
				}

				Assert.AreEqual(6, scene.Children.Count, "There should be 6 parts on the bed after the copy loop");

				// Remove an item
				testRunner.ClickByName("3D View Remove");

				// Confirm
				Assert.AreEqual(5, scene.Children.Count, "There should be 5 parts on the bed after remove");

				return Task.CompletedTask;
			}, overrideWidth:1300);
		}

		[Test]
		public async Task UndoRedoCopy()
		{
			await MatterControlUtilities.RunTest((testRunner) =>
			{
				testRunner.CloseSignInAndPrinterSelect();

				testRunner.AddItemToBedplate();

				var view3D = testRunner.GetWidgetByName("View3DWidget", out _) as View3DWidget;
				var scene = view3D.InteractionLayer.Scene;

				testRunner.Select3DPart("Calibration - Box.stl");

				Assert.AreEqual(1, scene.Children.Count, "There should be 1 part on the bed after AddDefaultFileToBedplate()");

				// Add 5 items
				for (int i = 0; i <= 4; i++)
				{
					testRunner.ClickByName("3D View Copy");
					testRunner.Delay(.5);
				}

				Assert.AreEqual(6, scene.Children.Count, "There should be 6 parts on the bed after the copy loop");

				// Perform and validate 5 undos
				for (int x = 0; x <= 4; x++)
				{
					int meshCountBeforeUndo = scene.Children.Count;
					testRunner.ClickByName("3D View Undo");

					testRunner.Delay(
						() => scene.Children.Count == meshCountBeforeUndo-1, 
						2);
					Assert.AreEqual(scene.Children.Count, meshCountBeforeUndo - 1);
				}

				testRunner.Delay(.2);

				// Perform and validate 5 redoes
				for (int z = 0; z <= 4; z++)
				{
					int meshCountBeforeRedo = scene.Children.Count;
					testRunner.ClickByName("3D View Redo");

					testRunner.Delay(
						() => meshCountBeforeRedo + 1 == scene.Children.Count,
						2);
					Assert.AreEqual(meshCountBeforeRedo + 1, scene.Children.Count);
				}

				return Task.CompletedTask;
			}, overrideWidth: 1300);
		}

		[Test]
		public async Task CopyRemoveUndoRedo()
		{
			await MatterControlUtilities.RunTest((testRunner) =>
			{
				testRunner.CloseSignInAndPrinterSelect();

				testRunner.AddItemToBedplate();

				// Get View3DWidget
				var view3D = testRunner.GetWidgetByName("View3DWidget", out _) as View3DWidget;
				var scene = view3D.InteractionLayer.Scene;

				testRunner.Select3DPart("Calibration - Box.stl");

				// Click Edit button to make edit controls visible
				testRunner.WaitForName("3D View Copy");
				testRunner.Delay(1); // wait for window to finish opening
				Assert.AreEqual(1, scene.Children.Count, "Should have 1 part before copy");

				for (int i = 0; i <= 4; i++)
				{
					testRunner.ClickByName("3D View Copy");
					testRunner.Delay(.2);
				}

				Assert.AreEqual(6, scene.Children.Count, "Should have 6 parts after batch copy");

				testRunner.ClickByName("3D View Remove");
				testRunner.Delay(() => scene.Children.Count == 5, 3);
				Assert.AreEqual(5, scene.Children.Count, "Should have 5 parts after Remove");

				testRunner.ClickByName("3D View Undo");
				testRunner.Delay(() => scene.Children.Count == 6, 3);
				Assert.AreEqual(6, scene.Children.Count, "Should have 6 parts after Undo");

				testRunner.ClickByName("3D View Redo");
				testRunner.Delay(() => scene.Children.Count == 5, 3);
				Assert.AreEqual(5, scene.Children.Count, "Should have 5 parts after Redo");

				view3D.CloseOnIdle();
				testRunner.Delay(.1);

				return Task.CompletedTask;
			}, overrideWidth: 1300);
		}

		[Test]
		public async Task ValidateDoUndoOnSceneOperations()
		{
			await MatterControlUtilities.RunTest((testRunner) =>
			{
				AutomationRunner.TimeToMoveMouse = .1;
				testRunner.CloseSignInAndPrinterSelect();
				testRunner.AddAndSelectPrinter("Airwolf 3D", "HD");

				var view3D = testRunner.GetWidgetByName("View3DWidget", out _) as View3DWidget;
				var scene1 = view3D.InteractionLayer.Scene;

				testRunner.NavigateToFolder("Calibration Parts Row Item Collection");

				/*
				// test un-group single mesh
				RunDoUndoTest(testRunner, scene1, (scene) =>
				{
					testRunner.AddItemToBedplate(partName: "Row Item MH Logo.stl");
					testRunner.Delay(.1);
					testRunner.ClickByName("MH Logo.stl");
					Assert.IsNotNull(scene.SelectedItem);
				},
				(scene) =>
				{
					testRunner.ClickByName("View3DWidget"); // place focus back in the scene
					testRunner.Type("^a"); // select all
					testRunner.ClickByName("3D View Ungroup");
					testRunner.ClickByName("View3DWidget"); // place focus back in the scene
					testRunner.Type(" "); // select none
					testRunner.Delay(() => scene1.Children.Count() == 3, .5);
					Assert.AreEqual(3, scene1.Children.Count());
				});
				*/

				// test group 2 objects
				RunDoUndoTest(testRunner, scene1, (scene) =>
				{
					AddBoxABoxBToBed(testRunner, scene);
					Assert.AreEqual(2, scene1.Children.Count());
				},
				(scene) =>
				{
					testRunner.ClickByName("View3DWidget"); // place focus back in the scene
					testRunner.Type("^a"); // select all
					testRunner.ClickByName("3D View Group");
					testRunner.ClickByName("View3DWidget"); // place focus back in the scene
					testRunner.Type(" "); // select none
					testRunner.Delay(() => scene1.Children.Count() == 1, .5);
					Assert.AreEqual(1, scene1.Children.Count());
					testRunner.Delay(() => scene.SelectedItem == null, .5);
				});

				// test un-group 2 grouped objects
				RunDoUndoTest(testRunner, scene1, (scene) =>
				{
					AddBoxABoxBToBed(testRunner, scene);
					Assert.AreEqual(2, scene1.Children.Count());
					testRunner.ClickByName("View3DWidget"); // place focus back in the scene
					testRunner.Type("^a"); // select all
					testRunner.ClickByName("3D View Group");
					testRunner.ClickByName("View3DWidget"); // place focus back in the scene
					testRunner.Type(" "); // select none
					testRunner.Delay(() => scene1.Children.Count() == 1, .5);
					Assert.AreEqual(1, scene1.Children.Count());
				},
				(scene) =>
				{
					testRunner.ClickByName("View3DWidget"); // place focus back in the scene
					testRunner.Type("^a"); // select all
					testRunner.ClickByName("3D View Ungroup");
					testRunner.ClickByName("View3DWidget"); // place focus back in the scene
					testRunner.Type(" "); // select none
					testRunner.Delay(() => scene1.Children.Count() == 2, .5);
					Assert.AreEqual(2, scene1.Children.Count());
				});

				// test mirror operations
				TestMirrorDoUndo(testRunner, scene1, "Mirror Button X");
				TestMirrorDoUndo(testRunner, scene1, "Mirror Button Y");
				TestMirrorDoUndo(testRunner, scene1, "Mirror Button Z");

				var coinName = "MatterControl - Coin.stl";
				// test drag x y translation
				RunDoUndoTest(testRunner, scene1, (scene) =>
				{
					AddCoinToBed(testRunner, scene);
				},
				(scene) =>
				{
					var part = testRunner.GetObjectByName(coinName, out _) as IObject3D;
					var start = part.GetAxisAlignedBoundingBox(Matrix4X4.Identity).Center;
					testRunner.DragDropByName(coinName, coinName, offsetDrop: new Point2D(40, 0));
					var end = part.GetAxisAlignedBoundingBox(Matrix4X4.Identity).Center;
					Assert.Greater(end.x, start.x);
					Assert.Less(end.y, start.y);
					Assert.True(Math.Abs(end.z - start.z) < .001);
				});

				// test z translation
				RunDoUndoTest(testRunner, scene1, (scene) =>
				{
					AddCoinToBed(testRunner, scene);
				},
				(scene) =>
				{
					var part = testRunner.GetObjectByName(coinName, out _) as IObject3D;
					var startZ = part.GetAxisAlignedBoundingBox(Matrix4X4.Identity).Center.z;
					// TODO: the offest drag is due to the aabb not being a great representation of the object, improve that and remove this offset.
					testRunner.DragDropByName("MoveInZControl", "MoveInZControl", offsetDrag: new Point2D(8, 0), offsetDrop: new Point2D(0, 40));
					var endZ = part.GetAxisAlignedBoundingBox(Matrix4X4.Identity).Center.z;
					Assert.Greater(endZ, startZ);
				});

				view3D.CloseOnIdle();
				testRunner.Delay(.1);

				return Task.CompletedTask;
			}, overrideWidth: 1300, maxTimeToRun: 200);
		}

		private static void AddCoinToBed(AutomationRunner testRunner, InteractiveScene scene)
		{
			testRunner.AddItemToBedplate(partName: "Row Item MatterControl - Coin.stl");
			testRunner.Delay(.1);
			// TODO: assert the part is centered on the bed

			testRunner.ClickByName("MatterControl - Coin.stl");
			Assert.IsNotNull(scene.SelectedItem);
		}

		private static void AddBoxABoxBToBed(AutomationRunner testRunner, InteractiveScene scene)
		{
			var item = "Calibration - Box.stl";
			testRunner.AddItemToBedplate(item);
			testRunner.Delay(.1);
			// move the first one over
			testRunner.DragDropByName(item, item, offsetDrop: new Point2D(40, 40));
			var part = testRunner.GetObjectByName(item, out _) as IObject3D;
			part.Name = "BoxA";

			testRunner.AddItemToBedplate();
			testRunner.Delay(.1);

			part = testRunner.GetObjectByName(item, out _) as IObject3D;
			part.Name = "BoxB";
		}

		private void TestMirrorDoUndo(AutomationRunner testRunner, InteractiveScene scene1, string mirrorButtonName)
		{
			RunDoUndoTest(testRunner, scene1, (scene) =>
			{
				AddCoinToBed(testRunner, scene);
			},
			(scene) =>
			{
				testRunner.ClickByName("Mirror Button");
				testRunner.ClickByName(mirrorButtonName);
			});
		}

		private void RunDoUndoTest(AutomationRunner testRunner,
			InteractiveScene scene, 
			Action<InteractiveScene> InitializeTest, 
			Action<InteractiveScene> PerformOpperation)
		{
			string scenePath = GetSceneTempPath();

			if (scene.Children.Count() > 0)
			{
				// clear the bed
				testRunner.ClickByName("Bed Options Menu");
				testRunner.ClickByName("Clear Bed Menu Item");
			}

			InitializeTest(scene);

			// save the scene
			string preOpperation = Path.Combine(scenePath, "preOpperation.mcx");
			scene.Save(preOpperation, scenePath);
			
			// Do the opperation
			PerformOpperation(scene);
			
			// save the scene
			string postOpperation = Path.Combine(scenePath, scenePath, "postOpperation.mcx");
			scene.Save(postOpperation, scenePath);

			// assert new save is different
			SceneFilesAreSame(postOpperation, preOpperation, false);

			// with the part selected
			AssertUndoRedo(testRunner, scene, scenePath, preOpperation, postOpperation);

			// unselect the part
			testRunner.ClickByName("View3DWidget"); // place focus back in the scene
			testRunner.Type(" "); // clear the selection (type a space)
			testRunner.Delay(() => scene.SelectedItem == null, .5);
			Assert.IsNull(scene.SelectedItem);

			// with the part unselected
			AssertUndoRedo(testRunner, scene, scenePath, preOpperation, postOpperation);
		}

		private void SceneFilesAreSame(string fileName1, string fileName2, bool expectedResult)
		{
			bool areSame = true;
			string[] fileContent1 = File.ReadAllLines(fileName1);
			string[] fileContent2 = File.ReadAllLines(fileName2);

			for (int i = 0; i < Math.Min(fileContent1.Length, fileContent2.Length); i++)
			{
				areSame &= ValidateSceneLine(fileContent1[i], fileContent2[i]);
				if (expectedResult)
				{
					Assert.IsTrue(areSame, $"Should be same ({i}): '{fileContent1[i]}' '{fileContent2[i]}");
				}
			}

			areSame &= fileContent1.Length == fileContent2.Length;
			if (expectedResult)
			{
				Assert.IsTrue(areSame, $"Should be same length: '{fileName1}' '{fileName2}");
			}

			Assert.IsTrue(expectedResult == areSame, $"Should be different: '{fileName1}' '{fileName2}");
		}

		private bool ValidateSceneLine(string v1, string v2)
		{
			if(v1 == v2)
			{
				return true;
			}

			if (v1.Contains("Matrix")
				&& v2.Contains("Matrix"))
			{
				double[] test = new double[] { 0, 1, 2, 3 };
				var expected = JsonConvert.SerializeObject(test, Formatting.Indented);

				// Figure out if the value content of these lines are equivelent.
				var data1 = v1.Substring(v1.IndexOf('['), v1.IndexOf(']') - v1.IndexOf('[') +  1);
				var matrix1 = new Matrix4X4(JsonConvert.DeserializeObject<double[]>(data1));
				var data2 = v2.Substring(v2.IndexOf('['), v2.IndexOf(']') - v2.IndexOf('[') + 1);
				var matrix2 = new Matrix4X4(JsonConvert.DeserializeObject<double[]>(data2));

				if(matrix1.Equals(matrix2, .001))
				{
					return true;
				}
			}

			return false;
		}

		private void AssertUndoRedo(AutomationRunner testRunner, InteractiveScene scene, string scenePath, string beforeEventScene, string afterEventScene)
		{
			// do an undo
			testRunner.ClickByName("3D View Undo");

			// save the undo data
			string undoScene = Path.Combine(scenePath, "undoScene.mcx");
			scene.Save(undoScene, scenePath);

			// assert postUndoScene == undoScene
			SceneFilesAreSame(beforeEventScene, undoScene, true);

			// now rede the undo
			testRunner.ClickByName("3D View Redo");

			// save the redo
			string redoScene = Path.Combine(scenePath, "redoScene.mcx");
			scene.Save(redoScene, scenePath);
			
			// assert postDoScene == doScene
			SceneFilesAreSame(afterEventScene, redoScene, true);
		}

		public static string GetSceneTempPath()
		{
			string tempPath = TestContext.CurrentContext.ResolveProjectPath(4, "Tests", "temp", "scenetests");
			Directory.CreateDirectory(tempPath);
			Object3D.AssetsPath = Path.Combine(tempPath, "Assets");

			return tempPath;
		}

		[Test]
		public async Task SaveAsToQueue()
		{
			await MatterControlUtilities.RunTest((testRunner) =>
			{
				testRunner.CloseSignInAndPrinterSelect();

				testRunner.AddItemToBedplate();

				var view3D = testRunner.GetWidgetByName("View3DWidget", out _) as View3DWidget;

				testRunner.Select3DPart("Calibration - Box.stl");

				for (int i = 0; i <= 2; i++)
				{
					testRunner.ClickByName("3D View Copy");
					testRunner.Delay(.5);
				}

				int expectedCount = QueueData.Instance.ItemCount + 1;

				testRunner.SaveBedplateToFolder("Test PartA", "Print Queue Row Item Collection");

				// Click Home -> Local Library
				testRunner.NavigateToLibraryHome();
				testRunner.NavigateToFolder("Print Queue Row Item Collection");

				Assert.IsTrue(testRunner.WaitForName("Row Item Test PartA", 5), "The part we added should be in the library");
				Assert.AreEqual(expectedCount, QueueData.Instance.ItemCount, "Queue count should increase by one after Save operation");

				return Task.CompletedTask;
			}, overrideWidth: 1300);
		}
	}
}
