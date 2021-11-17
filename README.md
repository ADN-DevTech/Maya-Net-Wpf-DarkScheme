## Dark theme for Maya WPF plug-in

Back to 2014, Cyrille wrote a [blog](https://around-the-corner.typepad.com/adn/2014/01/applying-the-maya-dark-color-scheme-to-wpf.html) about using XAML dictionary resouce to apply a dark theme for Maya WPF plug-in. In previous blog, it will create an application to load the theme. If more than one WPF plugin using the theme is loaded, it will cause an exception as only a single Application can exist within each AppDomain.

Although we can't use previous method to load the theme, the theme resource is still good for applying theme. We can use another method to load it without creating an application. It needs a little bit more extra work to setup than before:

1. Copy and add the Resources and Themes folder into your project. Please add the converters.cs to your project also.
2. Change all the files' Build Action file properties inside the Resources folder to Resources
3. Modify line 192 inside Themes/Skins/QadskDarkStyle.xaml, change **wpfexamples** to your assembly's name
````
<Setter Property="Icon" Value="/wpfexamples;component/Resources/maya.ico" />
````

Now, the setup is finished. If you want your window to use the dark theme provided with the resource dictionary, you'll need to include them inside your WPF window's XAML.

e.g.
````
    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Themes\Skins\QadskDarkStyle.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Window.Resources>
````

Then, you can set the windows's style to MayaStyle like below
````
<Window x:Class="wpfexamples.DAGExplorer"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="DAGExplorer" Height="465" Width="1420" Loaded="Window_Loaded" Margin="0,0,0,10"
        Style="{DynamicResource MayaStyle}">
````

Now, your WPF window will be using the Dark theme. Enjoy!
