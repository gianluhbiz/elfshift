using System.Collections.Generic;
using Elfshift.Core;
using NUnit.Framework;
using UnityEngine;

namespace Elfshift.Tests
{
    /// <summary>
    /// Test della logica delle ricette (senza GameObject).
    /// Per eseguirli: Window > General > Test Runner > EditMode > Run All.
    /// </summary>
    public sealed class RecipeMatcherTests
    {
        private ItemDefinition wood;
        private ItemDefinition cloth;
        private ItemDefinition metal;
        private ItemDefinition train;
        private ItemDefinition robot;
        private RecipeDefinition trainRecipe;
        private RecipeDefinition robotRecipe;
        private RecipeDefinition[] recipes;

        [SetUp]
        public void SetUp()
        {
            wood = NewItem("Legno", ItemState.RawMaterial);
            cloth = NewItem("Stoffa", ItemState.RawMaterial);
            metal = NewItem("Metallo", ItemState.RawMaterial);
            train = NewItem("Trenino", ItemState.Toy);
            robot = NewItem("Robot", ItemState.Toy);

            trainRecipe = ScriptableObject.CreateInstance<RecipeDefinition>();
            trainRecipe.EditorSetup(StationType.Construction, new[] { wood, wood }, train, 2f);
            robotRecipe = ScriptableObject.CreateInstance<RecipeDefinition>();
            robotRecipe.EditorSetup(StationType.Construction, new[] { metal, wood }, robot, 3f);
            recipes = new[] { trainRecipe, robotRecipe };
        }

        [TearDown]
        public void TearDown()
        {
            Object[] all = { wood, cloth, metal, train, robot, trainRecipe, robotRecipe };
            for (int i = 0; i < all.Length; i++) Object.DestroyImmediate(all[i]);
        }

        private static ItemDefinition NewItem(string name, ItemState state)
        {
            ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.EditorSetup(name.ToLowerInvariant(), name, state, null, Color.white);
            return item;
        }

        [Test]
        public void FindComplete_TwoWood_GivesTrain()
        {
            RecipeDefinition found = RecipeMatcher.FindComplete(recipes, StationType.Construction, new List<ItemDefinition> { wood, wood });
            Assert.AreSame(trainRecipe, found);
        }

        [Test]
        public void FindComplete_OrderDoesNotMatter()
        {
            RecipeDefinition a = RecipeMatcher.FindComplete(recipes, StationType.Construction, new List<ItemDefinition> { metal, wood });
            RecipeDefinition b = RecipeMatcher.FindComplete(recipes, StationType.Construction, new List<ItemDefinition> { wood, metal });
            Assert.AreSame(robotRecipe, a);
            Assert.AreSame(robotRecipe, b);
        }

        [Test]
        public void FindComplete_IncompleteOrWrongItems_ReturnsNull()
        {
            Assert.IsNull(RecipeMatcher.FindComplete(recipes, StationType.Construction, new List<ItemDefinition> { wood }));
            Assert.IsNull(RecipeMatcher.FindComplete(recipes, StationType.Construction, new List<ItemDefinition> { cloth, cloth }));
            Assert.IsNull(RecipeMatcher.FindComplete(recipes, StationType.Construction, new List<ItemDefinition>()));
        }

        [Test]
        public void FindComplete_WrongStation_ReturnsNull()
        {
            Assert.IsNull(RecipeMatcher.FindComplete(recipes, StationType.Paint, new List<ItemDefinition> { wood, wood }));
        }

        [Test]
        public void CanBePartOfRecipe_AcceptsUsefulAndRejectsUseless()
        {
            Assert.IsTrue(RecipeMatcher.CanBePartOfRecipe(recipes, StationType.Construction, new List<ItemDefinition> { wood }));
            Assert.IsTrue(RecipeMatcher.CanBePartOfRecipe(recipes, StationType.Construction, new List<ItemDefinition> { metal }));
            Assert.IsFalse(RecipeMatcher.CanBePartOfRecipe(recipes, StationType.Construction, new List<ItemDefinition> { cloth }));
            // tre legni non servono a nessuna ricetta
            Assert.IsFalse(RecipeMatcher.CanBePartOfRecipe(recipes, StationType.Construction, new List<ItemDefinition> { wood, wood, wood }));
            // legno + stoffa: nessuna ricetta le contiene insieme
            Assert.IsFalse(RecipeMatcher.CanBePartOfRecipe(recipes, StationType.Construction, new List<ItemDefinition> { wood, cloth }));
        }

        [Test]
        public void FormatTime_ShowsMinutesAndSeconds()
        {
            Assert.AreEqual("2:00", UiText.FormatTime(120));
            Assert.AreEqual("0:07", UiText.FormatTime(7));
            Assert.AreEqual("0:00", UiText.FormatTime(-3));
        }
    }
}
