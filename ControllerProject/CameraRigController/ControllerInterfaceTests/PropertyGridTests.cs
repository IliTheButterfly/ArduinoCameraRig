﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using FluentAssertions;
using CameraRigController.FieldGrid;
using System.ComponentModel;
using System.Linq;
using CameraRigController.FieldGrid.Editor.ViewModel;

namespace ControllerInterfaceTests
{
    [TestClass]
    public class PropertyGridTests
    {
        [DataTestMethod]
        [DataRow("ValueType", "Value type")]
        [DataRow("RGBColor", "RGB color")]
        [DataRow("Value1", "Value 1")]
        [DataRow("Value1b", "Value 1b")]
        [DataRow("Value1B", "Value 1 b")]
        [DataRow("Value15", "Value 15")]
        [DataRow("Value15b", "Value 15b")]
        [DataRow("Value15B", "Value 15 b")]
        [DataRow("Value15RGB", "Value 15 RGB")]
        [DataRow("Value15RGBGood1bTo5a", "Value 15 RGB good 1b to 5a")]
        [DataRow("MGHello24World", "MG hello 24 world")]

        public void TestNicifyNames(string value, string expected)
        {
            FieldGridUtillities.NicifyName(value).Should().Be(expected);
        }

        [TestMethod]
        public void TestSupportedTypes()
        {
            var t = new Type[]
            {
                typeof(string),
            };

            foreach (var type in t)
            {
                FieldGridUtillities.IsSupported(type);
            }
        }

        class DemoObject1 : INotifyPropertyChanged
        {
            private string _value1;

            public string Value1
            {
                get => _value1;
                set
                {
                    _value1 = value;
                    OnPropertyChanged(nameof(Value1));
                }
            }

            private string _value2;
            [ReadOnly(true)]
            public string Value2
            {
                get { return _value2; }
                set 
                { 
                    _value2 = value;
                    OnPropertyChanged(nameof(Value2));
                }
            }


            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

        }

        [TestMethod]
        public void TestStringDataConversion()
        {
            var obj = new DemoObject1() { Value1 = "Hello" };

            var collection = FieldGridUtillities.ToVMCollection(obj);
            collection[0].DisplayName.Should().Be("Value 1");
            collection[0].Should().BeOfType(typeof(StringEditorVM));
        }

        [TestMethod]
        public void TestStringDataBinding()
        {
            var obj = new DemoObject1() { Value1 = "Test1" };

            var collection = FieldGridUtillities.ToVMCollection(obj);
            var value1 = collection.First((p) => p.PropertyName == "Value1");

            value1.ObjectValue.Should().Be("Test1");

            value1.ObjectValue = "Test2";
            obj.Value1.Should().Be("Test2");

            obj.Value1 = "Test3";
            value1.ObjectValue.Should().Be("Test3");
        }

        [TestMethod]
        public void TestReadonlyStringDataConversion()
        {
            var obj = new DemoObject1() { Value2 = "Hello" };

            var collection = FieldGridUtillities.ToVMCollection(obj);
            var value2 = collection.First((p) => p.PropertyName == "Value2");

            value2.Should().BeOfType<ReadonlyStringEditorVM>();
            value2.ObjectValue.Should().Be("Hello");

            obj.Value2 = "World";
            value2.ObjectValue.Should().Be("World");
        }
    }
}