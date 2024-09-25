# WW.Cad_Net6.0_Examples

[CadLib 6.0/8.0 (multi-platorm)](https://www.woutware.com/net/cad/netcore/6.0) is the multi-platform version of CadLib,
a .NET library to read, write and display AutoCAD DWG and DXF files. 
Example projects include converters to PDF, SVG and bitmaps.

For the .NET version of CadLib for Windows only, please visit https://www.woutware.com/net/cad, and download the trial version, which contains Win Forms, WPF and Open GL viewer examples.

This repository contains basic read, write and export samples for the trial version of [CadLib 6.0 (multi-platorm) library](https://www.woutware.com/net/cad/netcore/6.0). 

For these applications to work you will need a trial license.
The MyAppKeyPair.snk linked in the projects are not present in the repository, 
you should generate your own strong name key and keep it private.

1. You can generate a strong name key with the following command in the Visual Studio command prompt:
    ```sn -k MyKeyPair.snk```

1. The next step is to extract the public key file from the strong name key (which is a key pair):
    ```sn -p MyKeyPair.snk MyPublicKey.snk```

1. Display the public key token for the public key: 	
    ```sn -t MyPublicKey.snk```

1. Go to the project properties Signing tab (or Build -> signing in VS2022), and check the "Sign the assembly" checkbox, 
   and choose the strong name key you created.

1. Register and get your trial license from [https://www.woutware.com/SoftwareLicenses](https://www.woutware.com/SoftwareLicenses).
   Enter your strong name key public key token that you got at step 3.
