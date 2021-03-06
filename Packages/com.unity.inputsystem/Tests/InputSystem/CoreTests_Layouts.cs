using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Processors;
using UnityEngine.Experimental.Input.Utilities;

partial class CoreTests
{
    [Test]
    [Category("Layouts")]
    public void Layouts_CanCreatePrimitiveControlsFromLayout()
    {
        var setup = new InputDeviceBuilder("Gamepad");

        // The default ButtonControl layout has no constrols inside of it.
        Assert.That(setup.GetControl("start"), Is.TypeOf<ButtonControl>());
        Assert.That(setup.GetControl("start").children, Is.Empty);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanCreateCompoundControlsFromLayout()
    {
        const int kNumControlsInAStick = 6;

        var setup = new InputDeviceBuilder("Gamepad");

        Assert.That(setup.GetControl("leftStick"), Is.TypeOf<StickControl>());
        Assert.That(setup.GetControl("leftStick").children, Has.Count.EqualTo(kNumControlsInAStick));
        Assert.That(setup.GetControl("leftStick").children, Has.Exactly(1).With.Property("name").EqualTo("x"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSetUpDeviceFromJsonLayout()
    {
        const string controlJson = @"
            {
                ""name"" : ""MyControl"",
                ""extend"" : ""Vector2""
            }
        ";
        const string deviceJson = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    {
                        ""name"" : ""myThing"",
                        ""layout"" : ""MyControl"",
                        ""usage"" : ""LeftStick""
                    }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(deviceJson);
        InputSystem.RegisterControlLayout(controlJson);

        var setup = new InputDeviceBuilder("MyDevice");

        Assert.That(setup.GetControl("myThing/x"), Is.TypeOf<AxisControl>());
        Assert.That(setup.GetControl("myThing"), Has.Property("layout").EqualTo("MyControl"));

        var device = setup.Finish();
        Assert.That(device, Is.TypeOf<InputDevice>());
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CannotUseLayoutForControlToBuildDevice()
    {
        Assert.That(() => new InputDeviceBuilder("Button"), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_UsagesOverrideThoseFromBaseLayout()
    {
        const string baseLayout = @"
            {
                ""name"" : ""BaseLayout"",
                ""controls"" : [
                    {
                        ""name"" : ""stick"",
                        ""layout"" : ""Stick"",
                        ""usage"" : ""BaseUsage""
                    },
                    {
                        ""name"" : ""axis"",
                        ""layout"" : ""Axis"",
                        ""usage"" : ""BaseUsage""
                    },
                    {
                        ""name"" : ""button"",
                        ""layout"" : ""Button""
                    }
                ]
            }
        ";
        const string derivedLayout = @"
            {
                ""name"" : ""DerivedLayout"",
                ""extend"" : ""BaseLayout"",
                ""controls"" : [
                    {
                        ""name"" : ""stick"",
                        ""usage"" : ""DerivedUsage""
                    },
                    {
                        ""name"" : ""axis""
                    },
                    {
                        ""name"" : ""button"",
                        ""usage"" : ""DerivedUsage""
                    }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(baseLayout);
        InputSystem.RegisterControlLayout(derivedLayout);

        var layout = InputSystem.TryLoadLayout("DerivedLayout");

        Assert.That(layout["stick"].usages.Count, Is.EqualTo(1));
        Assert.That(layout["stick"].usages, Has.Exactly(1).EqualTo(new InternedString("DerivedUsage")));
        Assert.That(layout["axis"].usages.Count, Is.EqualTo(1));
        Assert.That(layout["axis"].usages, Has.Exactly(1).EqualTo(new InternedString("BaseUsage")));
        Assert.That(layout["button"].usages.Count, Is.EqualTo(1));
        Assert.That(layout["button"].usages, Has.Exactly(1).EqualTo(new InternedString("DerivedUsage")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanModifyControlInBaseLayoutUsingPath()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick/x"",
                        ""format"" : ""BYTE""
                    }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(json);

        var setup = new InputDeviceBuilder("MyDevice");
        var device = (Gamepad)setup.Finish();

        Assert.That(device.leftStick.x.stateBlock.format, Is.EqualTo(InputStateBlock.kTypeByte));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSetControlParametersThroughControlAttribute()
    {
        // StickControl sets parameters on its axis controls. Check that they are
        // there.

        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(gamepad.leftStick.up.clamp, Is.True);
        Assert.That(gamepad.leftStick.up.clampMin, Is.EqualTo(0));
        Assert.That(gamepad.leftStick.up.clampMax, Is.EqualTo(1));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSetUsagesThroughControlAttribute()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(gamepad.leftStick.usages, Has.Exactly(1).EqualTo(CommonUsages.Primary2DMotion));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSetAliasesThroughControlAttribute()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(gamepad.buttonWest.aliases, Has.Exactly(1).EqualTo(new InternedString("square")));
        Assert.That(gamepad.buttonWest.aliases, Has.Exactly(1).EqualTo(new InternedString("x")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSetParametersOnControlInJson()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""rightTrigger"",
                        ""parameters"" : ""clamp=true,clampMin=0.123,clampMax=0.456""
                    }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(json);

        var device = (Gamepad)InputSystem.AddDevice("MyDevice");

        Assert.That(device.rightTrigger.clamp, Is.True);
        Assert.That(device.rightTrigger.clampMin, Is.EqualTo(0.123).Within(0.00001f));
        Assert.That(device.rightTrigger.clampMax, Is.EqualTo(0.456).Within(0.00001f));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanAddProcessorsToControlInJson()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick"",
                        ""processors"" : ""deadzone(min=0.1,max=0.9)""
                    }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(json);

        var device = (Gamepad)InputSystem.AddDevice("MyDevice");

        // NOTE: Unfortunately, this currently relies on an internal method (TryGetProcessor).

        Assert.That(device.leftStick.TryGetProcessor<DeadzoneProcessor>(), Is.Not.Null);
        Assert.That(device.leftStick.TryGetProcessor<DeadzoneProcessor>().min, Is.EqualTo(0.1).Within(0.00001f));
        Assert.That(device.leftStick.TryGetProcessor<DeadzoneProcessor>().max, Is.EqualTo(0.9).Within(0.00001f));
    }

    private unsafe struct StateStructWithArrayOfControls
    {
        [InputControl(layout = "Axis", arraySize = 5)]
        public fixed float value[5];
    }

    [InputControlLayout(stateType = typeof(StateStructWithArrayOfControls))]
    private class TestDeviceWithArrayOfControls : InputDevice
    {
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanAddArrayOfControls_InStateStruct()
    {
        InputSystem.RegisterControlLayout<TestDeviceWithArrayOfControls>();

        var device = new InputDeviceBuilder("TestDeviceWithArrayOfControls").Finish();

        Assert.That(device.allControls, Has.Count.EqualTo(5));
        Assert.That(device["value0"], Is.TypeOf<AxisControl>());
        Assert.That(device["value1"], Is.TypeOf<AxisControl>());
        Assert.That(device["value2"], Is.TypeOf<AxisControl>());
        Assert.That(device["value3"], Is.TypeOf<AxisControl>());
        Assert.That(device["value4"], Is.TypeOf<AxisControl>());
        Assert.That(device["value0"].stateBlock.byteOffset, Is.EqualTo(0 * sizeof(float)));
        Assert.That(device["value1"].stateBlock.byteOffset, Is.EqualTo(1 * sizeof(float)));
        Assert.That(device["value2"].stateBlock.byteOffset, Is.EqualTo(2 * sizeof(float)));
        Assert.That(device["value3"].stateBlock.byteOffset, Is.EqualTo(3 * sizeof(float)));
        Assert.That(device["value4"].stateBlock.byteOffset, Is.EqualTo(4 * sizeof(float)));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanAddArrayOfControls_InJSON()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    {
                        ""name"" : ""value"",
                        ""layout"" : ""Axis"",
                        ""arraySize"" : 5
                    }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(json);
        var device = new InputDeviceBuilder("MyDevice").Finish();

        Assert.That(device.allControls, Has.Count.EqualTo(5));
        Assert.That(device["value0"], Is.TypeOf<AxisControl>());
        Assert.That(device["value1"], Is.TypeOf<AxisControl>());
        Assert.That(device["value2"], Is.TypeOf<AxisControl>());
        Assert.That(device["value3"], Is.TypeOf<AxisControl>());
        Assert.That(device["value4"], Is.TypeOf<AxisControl>());
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_BooleanParameterDefaultsToTrueIfValueOmitted()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick/x"",
                        ""parameters"" : ""clamp""
                    }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(json);
        var device = (Gamepad) new InputDeviceBuilder("MyDevice").Finish();

        Assert.That(device.leftStick.x.clamp, Is.True);
    }

    [InputControlLayout(commonUsages = new[] {"LeftHand", "RightHand"})]
    private class DeviceWithCommonUsages : InputDevice
    {
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSpecifyCommonUsagesForDevices()
    {
        const string derivedJson = @"
            {
                ""name"" : ""DerivedDevice"",
                ""extend"" : ""BaseDevice"",
                ""commonUsages"" : [ ""LeftToe"" ]
            }
        ";

        InputSystem.RegisterControlLayout(typeof(DeviceWithCommonUsages), "BaseDevice");
        InputSystem.RegisterControlLayout(derivedJson);

        var layout = InputSystem.TryLoadLayout("DerivedDevice");

        Assert.That(layout.commonUsages, Has.Count.EqualTo(3));
        Assert.That(layout.commonUsages[0], Is.EqualTo(CommonUsages.LeftHand));
        Assert.That(layout.commonUsages[1], Is.EqualTo(CommonUsages.RightHand));
        Assert.That(layout.commonUsages[2], Is.EqualTo(new InternedString("LeftToe")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanFindLayoutFromDeviceDescription()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""device"" : {
                    ""product"" : ""mything.*""
                }
            }
        ";

        InputSystem.RegisterControlLayout(json);

        var layout = InputSystem.TryFindMatchingControlLayout(new InputDeviceDescription
        {
            product = "MyThingy"
        });

        Assert.That(layout, Is.EqualTo("MyDevice"));
    }

    [Test]
    [Category("Layout")]
    public void Layouts_CanOverrideLayoutMatchesForDiscoveredDevices()
    {
        InputSystem.onFindControlLayoutForDevice +=
            (int deviceId, ref InputDeviceDescription description, string layoutMatch, IInputRuntime runtime) =>
            "Keyboard";

        var device = InputSystem.AddDevice(new InputDeviceDescription {deviceClass = "Gamepad"});

        Assert.That(device, Is.TypeOf<Keyboard>());
    }

    // If a layout only specifies an interface in its descriptor, it is considered
    // a fallback for when there is no more specific layout that is able to match
    // by product.
    [Test]
    [Category("Layouts")]
    public void TODO_Layouts_CanHaveLayoutFallbackForInterface()
    {
        const string fallbackJson = @"
            {
                ""name"" : ""FallbackLayout"",
                ""device"" : {
                    ""interface"" : ""MyInterface""
                }
            }
        ";
        const string productJson = @"
            {
                ""name"" : ""ProductLayout"",
                ""device"" : {
                    ""interface"" : ""MyInterface"",
                    ""product"" : ""MyProduct""
                }
            }
        ";

        InputSystem.RegisterControlLayout(fallbackJson);
        InputSystem.RegisterControlLayout(productJson);

        Assert.Fail();
    }

    ////REVIEW: if this behavior is guaranteed, we also have to make sure we preserve it across domain reloads
    [Test]
    [Category("Layouts")]
    public void TODO_Layouts_WhenTwoLayoutsConflict_LastOneRegisteredWins()
    {
        const string firstLayout = @"
            {
                ""name"" : ""FirstLayout"",
                ""device"" : {
                    ""product"" : ""MyProduct""
                }
            }
        ";
        const string secondLayout = @"
            {
                ""name"" : ""SecondLayout"",
                ""device"" : {
                    ""product"" : ""MyProduct""
                }
            }
        ";

        InputSystem.RegisterControlLayout(firstLayout);
        InputSystem.RegisterControlLayout(secondLayout);

        var layout = InputSystem.TryFindMatchingControlLayout(new InputDeviceDescription {product = "MyProduct"});

        Assert.That(layout, Is.EqualTo("SecondLayout"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_AddingTwoControlsWithSameName_WillCauseException()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""MyControl"",
                        ""layout"" : ""Button""
                    },
                    {
                        ""name"" : ""MyControl"",
                        ""layout"" : ""Button""
                    }
                ]
            }
        ";

        // We do minimal processing when adding a layout so verification
        // only happens when we actually try to instantiate the layout.
        InputSystem.RegisterControlLayout(json);

        Assert.That(() => InputSystem.AddDevice("MyDevice"),
            Throws.TypeOf<Exception>().With.Property("Message").Contain("Duplicate control"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_ReplacingDeviceLayoutAffectsAllDevicesUsingLayout()
    {
        // Create a device hiearchy and then replace the base layout. We can't easily use
        // the gamepad (or something similar) as a base layout as it will use the Gamepad
        // class which will expect a number of controls to be present on the device.
        const string baseDeviceJson = @"
            {
                ""name"" : ""MyBase"",
                ""controls"" : [
                    { ""name"" : ""first"", ""layout"" : ""Button"" },
                    { ""name"" : ""second"", ""layout"" : ""Button"" }
                ]
            }
        ";
        const string derivedDeviceJson = @"
            {
                ""name"" : ""MyDerived"",
                ""extend"" : ""MyBase""
            }
        ";
        const string newBaseDeviceJson = @"
            {
                ""name"" : ""MyBase"",
                ""controls"" : [
                    { ""name"" : ""yeah"", ""layout"" : ""Stick"" }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(derivedDeviceJson);
        InputSystem.RegisterControlLayout(baseDeviceJson);

        var device = InputSystem.AddDevice("MyDerived");

        InputSystem.RegisterControlLayout(newBaseDeviceJson);

        Assert.That(device.children, Has.Count.EqualTo(1));
        Assert.That(device.children,
            Has.Exactly(1).With.Property("name").EqualTo("yeah").And.Property("layout").EqualTo("Stick"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_ReplacingDeviceLayoutWithLayoutUsingDifferentType_PreservesDeviceIdAndDescription()
    {
        const string initialJson = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""device"" : { ""product"" : ""Test"" }
            }
        ";

        InputSystem.RegisterControlLayout(initialJson);

        testRuntime.ReportNewInputDevice(new InputDeviceDescription {product = "Test"}.ToJson());
        InputSystem.Update();

        var oldDevice = InputSystem.devices.First(x => x.layout == "MyDevice");

        var oldDeviceId = oldDevice.id;
        var oldDeviceDescription = oldDevice.description;

        const string newJson = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Keyboard""
            }
        ";

        InputSystem.RegisterControlLayout(newJson);
        Assert.That(InputSystem.devices, Has.Exactly(1).With.Property("layout").EqualTo("MyDevice"));

        var newDevice = InputSystem.devices.First(x => x.layout == "MyDevice");

        Assert.That(newDevice.id, Is.EqualTo(oldDeviceId));
        Assert.That(newDevice.description, Is.EqualTo(oldDeviceDescription));
    }

    private class MyButtonControl : ButtonControl
    {
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_ReplacingControlLayoutAffectsAllDevicesUsingLayout()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // Replace "Button" layout.
        InputSystem.RegisterControlLayout<MyButtonControl>("Button");

        Assert.That(gamepad.leftTrigger, Is.TypeOf<MyButtonControl>());
    }

    private class TestLayoutType : Pointer
    {
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_RegisteringLayoutType_UsesBaseTypeAsBaseLayout()
    {
        InputSystem.RegisterControlLayout<TestLayoutType>();

        var layout = InputSystem.TryLoadLayout("TestLayoutType");

        Assert.That(layout.extendsLayout, Is.EqualTo("Pointer"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_RegisteringLayoutType_WithMatcher_PutsMatcherInLayoutWhenLoaded()
    {
        InputSystem.RegisterControlLayout<TestLayoutType>(
            matches: new InputDeviceMatcher()
            .WithInterface("TestInterface")
            .WithManufacturer("TestManufacturer")
            .WithProduct("TestProduct"));

        var layout = InputSystem.TryLoadLayout("TestLayoutType");

        Assert.That(layout.deviceMatcher.empty, Is.False);
        Assert.That(layout.deviceMatcher.patterns,
            Has.Exactly(1)
            .Matches<KeyValuePair<string, object>>(x => x.Key == "interface" && x.Value.Equals("TestInterface")));
        Assert.That(layout.deviceMatcher.patterns,
            Has.Exactly(1)
            .Matches<KeyValuePair<string, object>>(x => x.Key == "product" && x.Value.Equals("TestProduct")));
        Assert.That(layout.deviceMatcher.patterns,
            Has.Exactly(1)
            .Matches<KeyValuePair<string, object>>(x => x.Key == "interface" && x.Value.Equals("TestInterface")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_RegisteringLayoutBuilder_WithMatcher_PutsMatcherInLayoutWhenLoaded()
    {
        var builder = new TestLayoutBuilder {layoutToLoad = "Mouse"};

        InputSystem.RegisterControlLayoutBuilder(() => builder.DoIt(), name: "TestLayout",
            matches: new InputDeviceMatcher()
            .WithInterface("TestInterface")
            .WithProduct("TestProduct")
            .WithManufacturer("TestManufacturer"));

        var layout = InputSystem.TryLoadLayout("TestLayout");

        Assert.That(layout.deviceMatcher.empty, Is.False);
        Assert.That(layout.deviceMatcher.patterns,
            Has.Exactly(1)
            .Matches<KeyValuePair<string, object>>(x => x.Key == "interface" && x.Value.Equals("TestInterface")));
        Assert.That(layout.deviceMatcher.patterns,
            Has.Exactly(1)
            .Matches<KeyValuePair<string, object>>(x => x.Key == "product" && x.Value.Equals("TestProduct")));
        Assert.That(layout.deviceMatcher.patterns,
            Has.Exactly(1)
            .Matches<KeyValuePair<string, object>>(x => x.Key == "interface" && x.Value.Equals("TestInterface")));
    }

    // Want to ensure that if a state struct declares an "int" field, for example, and then
    // assigns it then Axis layout (which has a default format of float), the AxisControl
    // comes out with an "INT" format and not a "FLT" format.
    private struct StateStructWithPrimitiveFields : IInputStateTypeInfo
    {
        [InputControl(layout = "Axis")] public byte byteAxis;
        [InputControl(layout = "Axis")] public short shortAxis;

        [InputControl(layout = "Axis")] public int intAxis;

        // No float as that is the default format for Axis anyway.
        [InputControl(layout = "Axis")] public double doubleAxis;

        public FourCC GetFormat()
        {
            return new FourCC('T', 'E', 'S', 'T');
        }
    }

    [InputControlLayout(stateType = typeof(StateStructWithPrimitiveFields))]
    private class DeviceWithStateStructWithPrimitiveFields : InputDevice
    {
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_FormatOfControlWithPrimitiveTypeInStateStructInferredFromType()
    {
        InputSystem.RegisterControlLayout<DeviceWithStateStructWithPrimitiveFields>("Test");
        var setup = new InputDeviceBuilder("Test");

        Assert.That(setup.GetControl("byteAxis").stateBlock.format, Is.EqualTo(InputStateBlock.kTypeByte));
        Assert.That(setup.GetControl("shortAxis").stateBlock.format, Is.EqualTo(InputStateBlock.kTypeShort));
        Assert.That(setup.GetControl("intAxis").stateBlock.format, Is.EqualTo(InputStateBlock.kTypeInt));
        Assert.That(setup.GetControl("doubleAxis").stateBlock.format, Is.EqualTo(InputStateBlock.kTypeDouble));
    }

    private unsafe struct StateWithFixedArray : IInputStateTypeInfo
    {
        [InputControl] public fixed float buffer[2];

        public FourCC GetFormat()
        {
            return new FourCC('T', 'E', 'S', 'T');
        }
    }

    [InputControlLayout(stateType = typeof(StateWithFixedArray))]
    private class DeviceWithStateStructWithFixedArray : InputDevice
    {
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_FormatOfControlWithFixedArrayType_IsNotInferredFromType()
    {
        InputSystem.RegisterControlLayout<DeviceWithStateStructWithFixedArray>();

        Assert.That(() => new InputDeviceBuilder("DeviceWithStateStructWithFixedArray"),
            Throws.Exception.With.Message.Contain("Layout has not been set"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanHaveOneControlUseStateOfAnotherControl()
    {
        // It's useful to be able to say that control X should simply use the same state as control
        // Y. An example of this is the up/down/left/right controls of sticks that simply want to reuse
        // state from the x and y controls already on the stick. "useStateFrom" not only ensures that
        // if the state is moved around we move with it, it allows to redirect entire controls from
        // one part of the hierarchy to another part of the hierarchy (Touchscreen does that to point
        // the controls expected by the base Pointer class to the controls inside of "touch0").
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    { ""name"" : ""test"", ""layout"" : ""Axis"", ""useStateFrom"" : ""leftStick/x"" }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(json);

        var setup = new InputDeviceBuilder("MyDevice");
        var testControl = setup.GetControl<AxisControl>("test");
        var device = (Gamepad)setup.Finish();

        Assert.That(device.stateBlock.alignedSizeInBytes, Is.EqualTo(UnsafeUtility.SizeOf<GamepadState>()));
        Assert.That(testControl.stateBlock.byteOffset, Is.EqualTo(device.leftStick.x.stateBlock.byteOffset));
        Assert.That(testControl.stateBlock.sizeInBits, Is.EqualTo(device.leftStick.x.stateBlock.sizeInBits));
        Assert.That(testControl.stateBlock.format, Is.EqualTo(device.leftStick.x.stateBlock.format));
        Assert.That(testControl.stateBlock.bitOffset, Is.EqualTo(device.leftStick.x.stateBlock.bitOffset));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanAddChildControlToExistingControl()
    {
        const string json = @"
            {
                ""name"" : ""TestLayout"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    { ""name"" : ""leftStick/enabled"", ""layout"" : ""Button"" }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(json);
        var device = (Gamepad) new InputDeviceBuilder("TestLayout").Finish();

        ////TODO: this ignores layouting; ATM there's a conflict between the automatic layout used by the added button
        ////      and the manual layouting employed by Gamepad; we don't detect conflicts between manual and automatic
        ////      layouts yet so this goes undiagnosed

        Assert.That(device.leftStick.children, Has.Exactly(1).With.Property("name").EqualTo("enabled"));
        Assert.That(device.leftStick.children.Count, Is.EqualTo(device.rightStick.children.Count + 1));
        Assert.That(device.leftStick["enabled"].layout, Is.EqualTo("Button"));
        Assert.That(device.leftStick["enabled"].parent, Is.SameAs(device.leftStick));
    }

    [Test]
    [Category("Layouts")]
    public void TODO_Layouts_WhenModifyingChildControlsByPath_DependentControlsUsingStateFromAreUpdatedAsWell()
    {
        const string baseJson = @"
            {
                ""name"" : ""Base"",
                ""controls"" : [
                    { ""name"" : ""stick"", ""layout"" : ""Stick"" }
                ]
            }
        ";
        // When modifying the state layout of the X and Y controls of a stick from outside,
        // we also expect the up/down/left/right controls to get updated with the new state
        // layout.
        const string derivedJson = @"
            {
                ""name"" : ""Derived"",
                ""extend"" : ""Base"",
                ""controls"" : [
                    { ""name"" : ""stick/x"", ""format"" : ""SHRT"", ""offset"" : 0 },
                    { ""name"" : ""stick/y"", ""format"" : ""SHRT"", ""offset"" : 2 }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(baseJson);
        InputSystem.RegisterControlLayout(derivedJson);

        var setup = new InputDeviceBuilder("Derived");
        var stick = setup.GetControl<StickControl>("stick");

        Assert.That(stick.stateBlock.sizeInBits, Is.EqualTo(2 * 2 * 8));
    }

    ////TODO: write combined test that applies all possible modifications to a child control
    ////      (also add - and + capability)

    [Test]
    [Category("Layouts")]
    public void Layouts_CanModifyLayoutOfChildControlUsingPath()
    {
        const string json = @"
        {
            ""name"" : ""MyGamepad"",
            ""extend"" : ""Gamepad"",
            ""controls"" : [
                { ""name"" : ""dpad/up"", ""layout"" : ""DiscreteButton"" }
            ]
        }";

        InputSystem.RegisterControlLayout(json);

        var setup = new InputDeviceBuilder("MyGamepad");
        var gamepad = (Gamepad)setup.Finish();

        Assert.That(gamepad.dpad.up.layout, Is.EqualTo("DiscreteButton"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanModifyUsagesOfChildControlUsingPath()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    { ""name"" : ""stick"", ""layout"" : ""Stick"" },
                    { ""name"" : ""stick/x"", ""usage"" : ""TestUsage"" }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(json);

        var device = InputSystem.AddDevice("MyDevice");

        Assert.That(device["stick/x"].usages, Has.Exactly(1).EqualTo(new InternedString("TestUsage")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSpecifyDisplayNameForControl()
    {
        const string json = @"
            {
                ""name"" : ""MyLayout"",
                ""extend"" : ""Gamepad"",
                ""displayName"" : ""Test Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick"",
                        ""displayName"" : ""Primary Stick""
                    },
                    {
                        ""name"" : ""leftStick/x"",
                        ""displayName"" : ""Horizontal""
                    }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(json);

        var device = (Gamepad) new InputDeviceBuilder("MyLayout").Finish();

        Assert.That(device.displayName, Is.EqualTo("Test Gamepad"));
        Assert.That(device.leftStick.displayName, Is.EqualTo("Primary Stick"));
        Assert.That(device.leftStick.x.displayName, Is.EqualTo("Horizontal"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanMarkControlAsNoisy()
    {
        const string json = @"
            {
                ""name"" : ""MyLayout"",
                ""controls"" : [
                    {
                        ""name"" : ""button"",
                        ""layout"" : ""Button"",
                        ""noisy"" : true
                    }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(json);

        var device = InputSystem.AddDevice("MyLayout");

        Assert.That(device["button"].noisy, Is.True);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanBuildLayoutsInCode()
    {
        var builder = new InputControlLayout.Builder()
            .WithName("MyLayout")
            .WithType<Gamepad>()
            .Extend("Pointer")
            .WithFormat("CUST");

        builder.AddControl("button")
        .WithLayout("Button")
        .WithUsages("Foo", "Bar");

        var layout = builder.Build();

        Assert.That(layout.name.ToString(), Is.EqualTo("MyLayout"));
        Assert.That(layout.type, Is.SameAs(typeof(Gamepad)));
        Assert.That(layout.stateFormat, Is.EqualTo(new FourCC("CUST")));
        Assert.That(layout.extendsLayout, Is.EqualTo("Pointer"));
        Assert.That(layout.controls, Has.Count.EqualTo(1));
        Assert.That(layout.controls[0].name.ToString(), Is.EqualTo("button"));
        Assert.That(layout.controls[0].layout.ToString(), Is.EqualTo("Button"));
        Assert.That(layout.controls[0].usages.Count, Is.EqualTo(2));
        Assert.That(layout.controls[0].usages[0].ToString(), Is.EqualTo("Foo"));
        Assert.That(layout.controls[0].usages[1].ToString(), Is.EqualTo("Bar"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_BuildingLayoutInCode_WithEmptyUsageString_Throws()
    {
        var builder = new InputControlLayout.Builder().WithName("TestLayout");

        Assert.That(() => builder.AddControl("TestControl").WithUsages(""),
            Throws.ArgumentException.With.Message.Contains("TestControl")
            .And.With.Message.Contains("TestLayout"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_BuildingLayoutInCode_WithoutType_OnlySetsTypeIfNotExtendingLayout()
    {
        var builderExtendingLayout = new InputControlLayout.Builder()
            .WithName("TestLayout")
            .Extend("Pointer");
        var builderNotExtendingLayout = new InputControlLayout.Builder()
            .WithName("TestLayout");

        builderExtendingLayout.AddControl("button").WithLayout("Button");
        builderNotExtendingLayout.AddControl("button").WithLayout("Button");

        var layout1 = builderExtendingLayout.Build();
        var layout2 = builderNotExtendingLayout.Build();

        Assert.That(layout1.type, Is.Null);
        Assert.That(layout2.type, Is.SameAs(typeof(InputDevice)));
    }

    [Serializable]
    private class TestLayoutBuilder
    {
        [SerializeField] public string layoutToLoad;
        [NonSerialized] public InputControlLayout layout;

        public InputControlLayout DoIt()
        {
            // To make this as simple as possible, just load another layout.
            layout = InputSystem.TryLoadLayout(layoutToLoad);
            return layout;
        }
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanAddCustomLayoutBuilder()
    {
        var builder = new TestLayoutBuilder {layoutToLoad = "Gamepad"};

        InputSystem.RegisterControlLayoutBuilder(() => builder.DoIt(), "MyLayout");

        var result = InputSystem.TryLoadLayout("MyLayout");

        Assert.That(result.name.ToString(), Is.EqualTo("MyLayout"));
        Assert.That(result, Is.SameAs(builder.layout));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanTurnLayoutIntoJson()
    {
        var layout = InputSystem.TryLoadLayout("Gamepad");
        var json = layout.ToJson();
        var deserializedLayout = InputControlLayout.FromJson(json);

        Assert.That(deserializedLayout.name, Is.EqualTo(layout.name));
        Assert.That(deserializedLayout.controls, Has.Count.EqualTo(layout.controls.Count));
        Assert.That(deserializedLayout.stateFormat, Is.EqualTo(layout.stateFormat));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanGetControlLayoutFromControlPath()
    {
        InputSystem.AddDevice("gamepad"); // Just to make sure we don't use this.

        // Control layout mentioned explicitly.
        Assert.That(InputControlPath.TryGetControlLayout("*/<button>"),
            Is.EqualTo("button")); // Does not "correct" casing.
        // Control layout can be looked up from device layout.
        Assert.That(InputControlPath.TryGetControlLayout("/<gamepad>/leftStick"), Is.EqualTo("Stick"));
        // With multiple controls, only returns result if all controls use the same layout.
        Assert.That(InputControlPath.TryGetControlLayout("/<gamepad>/*Stick"), Is.EqualTo("Stick"));
        // Except if we match all controls on the device in which case it's taken to mean "any layout goes".
        Assert.That(InputControlPath.TryGetControlLayout("/<gamepad>/*"), Is.EqualTo("*"));
        ////TODO
        // However, having a wildcard on the device path is taken to mean "all device layouts" in this case.
        //Assert.That(InputControlPath.TryGetControlLayout("/*/*Stick"), Is.EqualTo("Stick"));
        // Can determine layout used by child control.
        Assert.That(InputControlPath.TryGetControlLayout("<gamepad>/leftStick/x"), Is.EqualTo("Axis"));
        // Can determine layout from control with usage.
        Assert.That(InputControlPath.TryGetControlLayout("<gamepad>/{PrimaryAction}"), Is.EqualTo("Button"));
        // Will not look up from instanced devices at runtime so can't know device layout from this path.
        Assert.That(InputControlPath.TryGetControlLayout("/gamepad/leftStick"), Is.Null);
        // If only a device layout is given, can't know control layout.
        Assert.That(InputControlPath.TryGetControlLayout("/<gamepad>"), Is.Null);

        ////TODO: make sure we can find layouts from control layout modifying child paths
        ////TODO: make sure that finding by usage can look arbitrarily deep into the hierarchy
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanGetDeviceLayoutFromControlPath()
    {
        InputSystem.AddDevice("gamepad"); // Just to make sure we don't use this.

        Assert.That(InputControlPath.TryGetDeviceLayout("<gamepad>/leftStick"), Is.EqualTo("gamepad"));
        Assert.That(InputControlPath.TryGetDeviceLayout("/<gamepad>"), Is.EqualTo("gamepad"));
        Assert.That(InputControlPath.TryGetDeviceLayout("/*/*Stick"), Is.EqualTo("*"));
        Assert.That(InputControlPath.TryGetDeviceLayout("/*"), Is.EqualTo("*"));
        Assert.That(InputControlPath.TryGetDeviceLayout("/gamepad/leftStick"), Is.Null);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanLoadLayout()
    {
        var json = @"
            {
                ""name"" : ""MyLayout"",
                ""controls"" : [ { ""name"" : ""MyControl"" } ]
            }
        ";

        InputSystem.RegisterControlLayout(json);

        var jsonLayout = InputSystem.TryLoadLayout("MyLayout");

        Assert.That(jsonLayout, Is.Not.Null);
        Assert.That(jsonLayout.name, Is.EqualTo(new InternedString("MyLayout")));
        Assert.That(jsonLayout.controls, Has.Count.EqualTo(1));
        Assert.That(jsonLayout.controls[0].name, Is.EqualTo(new InternedString("MyControl")));

        var gamepadLayout = InputSystem.TryLoadLayout("Gamepad");

        Assert.That(gamepadLayout, Is.Not.Null);
        Assert.That(gamepadLayout.name, Is.EqualTo(new InternedString("Gamepad")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanRemoveLayout()
    {
        var json = @"
            {
                ""name"" : ""MyLayout"",
                ""extend"" : ""Gamepad""
            }
        ";

        InputSystem.RegisterControlLayout(json);
        var device = InputSystem.AddDevice("MyLayout");

        Assert.That(InputSystem.ListControlLayouts(), Has.Exactly(1).EqualTo("MyLayout"));
        Assert.That(InputSystem.devices, Has.Exactly(1).SameAs(device));

        InputSystem.RemoveControlLayout("MyLayout");

        Assert.That(InputSystem.ListControlLayouts(), Has.None.EqualTo("MyLayout"));
        Assert.That(InputSystem.devices, Has.None.SameAs(device));
        Assert.That(InputSystem.devices, Has.None.With.Property("layout").EqualTo("MyLayout"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_ChangingLayouts_SendsNotifications()
    {
        InputControlLayoutChange? receivedChange = null;
        string receivedLayout = null;

        InputSystem.onControlLayoutChange +=
            (layout, change) =>
            {
                receivedChange = change;
                receivedLayout = layout;
            };

        const string jsonV1 = @"
            {
                ""name"" : ""MyLayout"",
                ""extend"" : ""Gamepad""
            }
        ";

        // Add layout.
        InputSystem.RegisterControlLayout(jsonV1);

        Assert.That(receivedChange, Is.EqualTo(InputControlLayoutChange.Added));
        Assert.That(receivedLayout, Is.EqualTo("MyLayout"));

        const string jsonV2 = @"
            {
                ""name"" : ""MyLayout"",
                ""extend"" : ""Keyboard""
            }
        ";

        receivedChange = null;
        receivedLayout = null;

        // Change layout.
        InputSystem.RegisterControlLayout(jsonV2);

        Assert.That(receivedChange, Is.EqualTo(InputControlLayoutChange.Replaced));
        Assert.That(receivedLayout, Is.EqualTo("MyLayout"));

        receivedChange = null;
        receivedLayout = null;

        // RemoveControlLayout.
        InputSystem.RemoveControlLayout("MyLayout");

        Assert.That(receivedChange, Is.EqualTo(InputControlLayoutChange.Removed));
        Assert.That(receivedLayout, Is.EqualTo("MyLayout"));
    }

    [Test]
    [Category("Layouts")]
    public void TODO_Layouts_RemovingLayouts_RemovesAllLayoutsBasedOnIt()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Layouts")]
    public void TODO_Layouts_CanQueryResourceNameFromControl()
    {
        var json = @"
            {
                ""name"" : ""MyLayout"",
                ""controls"" : [ { ""name"" : ""MyControl"",  } ]
            }
        ";

        InputSystem.RegisterControlLayout(json);

        Assert.Fail();
    }

    private struct StateWithTwoLayoutVariants : IInputStateTypeInfo
    {
        [InputControl(name = "button", layout = "Button", variant = "A")]
        public int buttons;

        [InputControl(name = "axis", layout = "Axis", variant = "B")]
        public float axis;

        public FourCC GetFormat()
        {
            return new FourCC('T', 'E', 'S', 'T');
        }
    }

    [InputControlLayout(variant = "A", stateType = typeof(StateWithTwoLayoutVariants))]
    private class DeviceWithLayoutVariantA : InputDevice
    {
    }

    [InputControlLayout(variant = "B", stateType = typeof(StateWithTwoLayoutVariants))]
    private class DeviceWithLayoutVariantB : InputDevice
    {
    }

    // Sometimes you have a single state format that you want to use with multiple
    // different types of devices that each have a different control setup. For example,
    // a given state may just be a generic set of axis and button values with the
    // assignment of axis and button controls depending on which type of device the
    // state is used with.
    [Test]
    [Category("Layouts")]
    public void Layouts_CanSetUpMultipleLayoutsFromSingleState_UsingVariants()
    {
        InputSystem.RegisterControlLayout<DeviceWithLayoutVariantA>();
        InputSystem.RegisterControlLayout<DeviceWithLayoutVariantB>();

        var deviceA = InputSystem.AddDevice<DeviceWithLayoutVariantA>();
        var deviceB = InputSystem.AddDevice<DeviceWithLayoutVariantB>();

        Assert.That(deviceA.allControls.Count, Is.EqualTo(1));
        Assert.That(deviceB.allControls.Count, Is.EqualTo(1));

        Assert.That(deviceA["button"], Is.TypeOf<ButtonControl>());
        Assert.That(deviceB["axis"], Is.TypeOf<AxisControl>());

        Assert.That(deviceA["button"].variant, Is.EqualTo("A"));
        Assert.That(deviceB["axis"].variant, Is.EqualTo("B"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_SettingVariantOnLayout_MergesAwayNonMatchingControlInformationFromBaseLayouts()
    {
        const string jsonBase = @"
            {
                ""name"" : ""BaseLayout"",
                ""extend"" : ""DeviceWithLayoutVariantA"",
                ""controls"" : [
                    { ""name"" : ""ControlFromBase"", ""layout"" : ""Button"" },
                    { ""name"" : ""OtherControlFromBase"", ""layout"" : ""Axis"" },
                    { ""name"" : ""ControlWithExplicitDefaultVariant"", ""layout"" : ""Axis"", ""variant"" : ""default"" },
                    { ""name"" : ""StickControl"", ""layout"" : ""Stick"" },
                    { ""name"" : ""StickControl/x"", ""offset"" : 14, ""variant"" : ""A"" }
                ]
            }
        ";
        const string jsonDerived = @"
            {
                ""name"" : ""DerivedLayout"",
                ""extend"" : ""BaseLayout"",
                ""controls"" : [
                    { ""name"" : ""ControlFromBase"", ""variant"" : ""A"", ""offset"" : 20 }
                ]
            }
        ";

        InputSystem.RegisterControlLayout<DeviceWithLayoutVariantA>();
        InputSystem.RegisterControlLayout(jsonBase);
        InputSystem.RegisterControlLayout(jsonDerived);

        var layout = InputSystem.TryLoadLayout("DerivedLayout");

        // The variant setting here is coming all the way from the base layout so itself already has
        // to come through properly in the merge.
        Assert.That(layout.variant, Is.EqualTo(new InternedString("A")));

        // Not just the variant setting itself should come through but it also should affect the
        // merging of control items. `ControlFromBase` has a layout set on it which should get picked
        // up by the variant defined for it in `DerivedLayout`. Also, controls that don't have the right
        // variant should have been removed.
        Assert.That(layout.controls.Count, Is.EqualTo(6));
        Assert.That(layout.controls, Has.None.Matches<InputControlLayout.ControlItem>(
                x => x.name == new InternedString("axis"))); // Axis control should have disappeared.
        Assert.That(layout.controls, Has.Exactly(1).Matches<InputControlLayout.ControlItem>(
                x => x.name == new InternedString(
                    "OtherControlFromBase")));  // But this one targeting no specific variant should be included.
        Assert.That(layout.controls, Has.Exactly(1)
            .Matches<InputControlLayout.ControlItem>(x =>
                x.name == new InternedString("ControlFromBase") && x.layout == new InternedString("Button") &&
                x.offset == 20 && x.variant == new InternedString("A")));
        Assert.That(layout.controls, Has.Exactly(1)
            .Matches<InputControlLayout.ControlItem>(x =>
                x.name == new InternedString("ControlWithExplicitDefaultVariant")));
        // Make sure that the "StickControl/x" item came through along with the stick itself.
        Assert.That(layout.controls, Has.Exactly(1)
            .Matches<InputControlLayout.ControlItem>(x =>
                x.name == new InternedString("StickControl")));
        Assert.That(layout.controls, Has.Exactly(1)
            .Matches<InputControlLayout.ControlItem>(x =>
                x.name == new InternedString("StickControl/x")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanReformatAndResizeControlHierarchy()
    {
        // Turn left stick into a 2D vector of shorts.
        // NOTE: Child offsets are not absolute! They are relative to their parent.
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    { ""name"" : ""leftStick"", ""format"" : ""VC2S"", ""offset"" : 6 },
                    { ""name"" : ""leftStick/x"", ""format"" : ""SHRT"", ""offset"" : 0 },
                    { ""name"" : ""leftStick/y"", ""format"" : ""SHRT"", ""offset"" : 2 },
                    { ""name"" : ""leftStick/up"", ""format"" : ""SHRT"", ""offset"" : 2 },
                    { ""name"" : ""leftStick/down"", ""format"" : ""SHRT"", ""offset"" : 2 },
                    { ""name"" : ""leftStick/left"", ""format"" : ""SHRT"", ""offset"" : 0 },
                    { ""name"" : ""leftStick/right"", ""format"" : ""SHRT"", ""offset"" : 0 }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(json);
        var device = (Gamepad) new InputDeviceBuilder("MyDevice").Finish();

        Assert.That(device.leftStick.stateBlock.byteOffset, Is.EqualTo(6));
        Assert.That(device.leftStick.stateBlock.sizeInBits, Is.EqualTo(2 * 2 * 8));
        Assert.That(device.leftStick.x.stateBlock.byteOffset, Is.EqualTo(6));
        Assert.That(device.leftStick.y.stateBlock.byteOffset, Is.EqualTo(8));
        Assert.That(device.leftStick.x.stateBlock.sizeInBits, Is.EqualTo(2 * 8));
        Assert.That(device.leftStick.x.stateBlock.sizeInBits, Is.EqualTo(2 * 8));
        Assert.That(device.leftStick.up.stateBlock.byteOffset, Is.EqualTo(8));
        Assert.That(device.leftStick.up.stateBlock.sizeInBits, Is.EqualTo(2 * 8));
        Assert.That(device.leftStick.down.stateBlock.byteOffset, Is.EqualTo(8));
        Assert.That(device.leftStick.down.stateBlock.sizeInBits, Is.EqualTo(2 * 8));
        Assert.That(device.leftStick.left.stateBlock.byteOffset, Is.EqualTo(6));
        Assert.That(device.leftStick.left.stateBlock.sizeInBits, Is.EqualTo(2 * 8));
        Assert.That(device.leftStick.right.stateBlock.byteOffset, Is.EqualTo(6));
        Assert.That(device.leftStick.right.stateBlock.sizeInBits, Is.EqualTo(2 * 8));
    }

    ////TODO: This test doesn't yet make sense. The thought of how the feature should work is
    ////      correct, but the setup makes no sense and doesn't work. Gamepad adds deadzones
    ////      on the *sticks* so modifying that requires a Vector2 type processor which invert
    ////      isn't.
    [Test]
    [Category("Layouts")]
    public void TODO_Layouts_CanMoveProcessorFromBaseLayoutInProcessorStack()
    {
        // The base gamepad layout is adding deadzone processors to sticks. However, a
        // layout based on that one may want to add processors *before* deadzoning is
        // applied. It can do so very simply by listing where the deadzone processor should
        // occur.
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick/x"",
                        ""processors"" : ""invert,deadzone""
                    }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(json);

        var setup = new InputDeviceBuilder("MyDevice");
        var leftStickX = setup.GetControl<AxisControl>("leftStick/x");

        Assert.That(leftStickX.processors, Has.Length.EqualTo(2));
        Assert.That(leftStickX.processors[0], Is.TypeOf<InvertProcessor>());
        Assert.That(leftStickX.processors[1], Is.TypeOf<DeadzoneProcessor>());
    }

    [Test]
    [Category("Layouts")]
    public void TODO_Layout_CustomizedStateLayoutWillNotUseFormatCodeFromBaseLayout()
    {
        //make sure that if you customize a gamepad layout, you don't end up with the "GPAD" format on the device
        //in fact, the system should require a format code to be specified in that case
        Assert.Fail();
    }

    ////REVIEW: This one seems like it adds quite a bit of complexity for somewhat minor gain.
    ////        May even be safer to *not* support this as it may inject controls at offsets where you don't expect them.
    //[InputControl(name = "axis", offset = InputStateBlock.kInvalidOffset)]
    private struct BaseInputState : IInputStateTypeInfo
    {
        [InputControl(layout = "Axis")] public float axis;
        public int padding;
        public FourCC GetFormat()
        {
            return new FourCC("BASE");
        }
    }

    [InputControlLayout(stateType = typeof(BaseInputState))]
    private class BaseInputDevice : InputDevice
    {
    }

    private struct DerivedInputState : IInputStateTypeInfo
    {
        public FourCC GetFormat()
        {
            return new FourCC("DERI");
        }
    }

    [InputControlLayout(stateType = typeof(DerivedInputState))]
    private class DerivedInputDevice : InputDevice
    {
    }

    [Test]
    [Category("Layouts")]
    public void TODO_Layouts_InputStateInDerivedClassMergesWithControlsOfInputStateFromBaseClass()
    {
        //axis should appear in DerivedInputDevice and should have been moved to offset 8 (from automatic assignment)
        Assert.Fail();
    }
}
