using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.IO;
using System.Xml;
using System.Collections;
using System.Globalization;
using System.Drawing;
using System.Threading;

public class CarPersistence : Script


    World.getAllVehicles ????


    /*

    maybe need for restructure

    */

    // General: exception handling in c# opposed to java
    // add config

    // if no vehicles are found and atleast if file is invalid, the make backup copy so user could correct it
    // create new copy so the mod will still function --> needs testing for example when exceptions will be created and so on

    // fix bug where vehicles only seem to load after an manual reload
    // DEEP Copy so it not disapperas from list when despwans
    // look at string to mod and find a solution if sztring cannot be parsed
    // primary vheicle color vs custom primary color.. ?!
    // unload vehicles before reload
    // check if place where vehicle should be spawned is occupied .. rather move occupying object than delete
    // maybe implement logging
    // have a look at vehicle count that needs to be spawned .. possible conflict because only so many vehicles can be spawned at once ?
    // map markers ! 

    /*
        despawn old vehicles
         get Entitys in ceratin area
         look if vehicle is und

    */
    

    // ArrayList .. keep track of vehicles by list and not file ? if so deep copy is needed because otherwhise values would change

    // not working mods:
    // lights
    // wheels
    // numberplate
    // turbo
    // color if not custom.. choose color from avaible gta colors
    // window tint

{
    String scriptsPath = "F:/GTA V Mod Manager/Mods/scripts"; // --> path to scripts folder

    String directoryPath;
    String filePath;

    Ped player;
    List<XElement> savedVehicles = new List<XElement>();
    List<XElement> savedVehicles_SpawnedDriven = new List<XElement>();
    XElement xElement;

    // testing

    List<Vehicle> saved = new List<Vehicle>();


    int spawnDistance = 10; // M
    int checkFrequenzy = 250; // MS

    public CarPersistence() {

        directoryPath = scriptsPath + "/CarPersistence"; // Ordner für Dateien, relativ zum Speicherort des Scripts
        filePath = directoryPath + "/Vehicles.xml"; // Datei, die alle gespeicherten Fahrzeuge enthält

        onLoad(); // seems to work

        Tick += OnTick;
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;

        Interval = checkFrequenzy;
    }

    private void onLoad()
    {
        // Check if file exists --> if not create
        checkSaveData(filePath);

        player = Game.Player.Character;
        loadSavedVehicels(filePath);
    }

    private void checkSaveData(string path) {

        if (!Directory.Exists(directoryPath)) {
            Directory.CreateDirectory(directoryPath);
            XElement xElement = new XElement("SavedVehicles");
            xElement.Save(filePath);
        } else {
            if (!File.Exists(filePath)) {
                XElement xElement = new XElement("SavedVehicles");
                xElement.Save(filePath);
            }
        }
    }

    private void loadSavedVehicels(String filePath)
    {
        xElement = XElement.Load(filePath);
        IEnumerable vehicleElements = xElement.Elements("Vehicle");

        UI.Notify("Loading Vehicles..");
        int count = 0;

        foreach(XElement vehicle in vehicleElements)
        {
            savedVehicles.Add(vehicle);
            count++;
        }

        if (count > 0) {
            UI.Notify("Loading done. [" + count + "] Vehicles loaded!");
        } else {
            UI.Notify("No vehicles were found.");
        }
    }

    private void spawnVehiclesInProximity() {

        XElement location;
        float x;
        float y; // move variables because of speed 4times a second..
        foreach (XElement vehicle in savedVehicles) {

            location = vehicle.Element("Coordinates");
            x = Convert.ToSingle(location.Element("X").Value, CultureInfo.InvariantCulture);
            y = Convert.ToSingle(location.Element("Y").Value, CultureInfo.InvariantCulture);

            // Does succesfully spawn the vehicle and moves it from saved to active
            if (((x - player.Position.X) <= spawnDistance) || ((x - player.Position.X) >= (spawnDistance * -1))) {
                spawnVehicle(vehicle);
                savedVehicles_SpawnedDriven.Add(vehicle);
                savedVehicles.Remove(vehicle);
            } else if (((y - player.Position.Y) <= spawnDistance) || ((y - player.Position.Y) >= (spawnDistance * -1))) {
                spawnVehicle(vehicle);
                savedVehicles_SpawnedDriven.Add(vehicle);
                savedVehicles.Remove(vehicle);
            }
        }
    }

    private void cleanUpActiveVehicles() {

        XElement location;
        float x;
        float y; // move variables because of speed 4times a second..
        foreach (XElement vehicle in savedVehicles_SpawnedDriven) {

            location = vehicle.Element("Coordinates");
            x = Convert.ToSingle(location.Element("X").Value, CultureInfo.InvariantCulture);
            y = Convert.ToSingle(location.Element("Y").Value, CultureInfo.InvariantCulture);

            // Does succesfully spawn the vehicle and moves it from saved to active
            if (((x - player.Position.X) > spawnDistance) || ((x - player.Position.X) < (spawnDistance * -1))) {
                spawnVehicle(vehicle);
                savedVehicles.Add(vehicle);
                savedVehicles_SpawnedDriven.Remove(vehicle);
            } else if (((y - player.Position.Y) > spawnDistance) || ((y - player.Position.Y) < (spawnDistance * -1))) {
                spawnVehicle(vehicle);
                savedVehicles.Add(vehicle);
                savedVehicles_SpawnedDriven.Remove(vehicle);
            }
        }
    }

    private void spawnVehicle(XElement v)
    {
        UI.Notify("Requested spawn of vehicle: " + v.Element("VehicleName").Value);

        XElement location = v.Element("Coordinates");
        float x = Convert.ToSingle(location.Element("X").Value, CultureInfo.InvariantCulture);
        float y = Convert.ToSingle(location.Element("Y").Value, CultureInfo.InvariantCulture);
        float z = Convert.ToSingle(location.Element("Z").Value, CultureInfo.InvariantCulture);
        Vector3 loc = new Vector3(x, y, z);

        float heading = Convert.ToSingle(location.Element("Heading").Value, CultureInfo.InvariantCulture);

        // Vehicle spawn by name
        Vehicle ve = World.CreateVehicle(new Model(v.Element("VehicleName").Value), loc);
        ve.Heading = heading;
        ve.InstallModKit(); // --> needed so mods can be installed !

        string[] xs = Enum.GetNames(typeof(VehicleMod));

        for (int i = 0; i < xs.Length; i++)
        {
            ve.SetMod(stringToMod(xs[i]), (int.Parse(v.Element(xs[i]).Value)), false);
        }

        XElement colorElement = v.Element("Color");

        int red = int.Parse(colorElement.Element("CustomPrimaryColor").Element("Red").Value);
        int green = int.Parse(colorElement.Element("CustomPrimaryColor").Element("Green").Value);
        int blue = int.Parse(colorElement.Element("CustomPrimaryColor").Element("Blue").Value);

        ve.CustomPrimaryColor = Color.FromArgb(255, red, green, blue);

        red = int.Parse(colorElement.Element("CustomSecondaryColor").Element("Red").Value);
        green = int.Parse(colorElement.Element("CustomSecondaryColor").Element("Green").Value);
        blue = int.Parse(colorElement.Element("CustomSecondaryColor").Element("Blue").Value);

        ve.CustomSecondaryColor = Color.FromArgb(255, red, green, blue);

        ve.PlaceOnGround();

        UI.Notify("Vehicle spawned");
    }

    private GTA.VehicleMod stringToMod(string s)
    {
        GTA.VehicleMod vehicleMod;
        if (Enum.TryParse<GTA.VehicleMod>(s, out vehicleMod))
        {
            if (Enum.IsDefined(typeof(GTA.VehicleMod), vehicleMod))
            {
                return vehicleMod;
            }
            else
            {
                return 0;
            }
        }
        else
        {
            // illegal argument exception
            // skip vehicle // abort laoding
            return 0;
        }
    }

    private void saveVehicle(Vehicle v) {

        UI.Notify("Trying to save vehicle. [" + v.DisplayName + "]");
        XElement vehicleElement = new XElement("Vehicle",
                        new XElement("VehicleName", v.DisplayName),
                        new XElement("VehicleModel", v.Model),
                        new XElement("Coordinates",
                            new XElement("X", v.Position.X),
                            new XElement("Y", v.Position.Y),
                            new XElement("Z", v.Position.Z),
                            new XElement("Heading", v.Heading)),
                        new XElement("Color",
                            new XElement("PrimaryColor",
                                new XElement("Red", v.PrimaryColor)),
                            new XElement("SecondaryColor",
                                new XElement("Red", v.SecondaryColor)),
                           new XElement("CustomPrimaryColor",
                                new XElement("Red", v.CustomPrimaryColor.R),
                                new XElement("Green", v.CustomPrimaryColor.G),
                                new XElement("Blue", v.CustomPrimaryColor.B)),
                            new XElement("CustomSecondaryColor",
                                new XElement("Red", v.CustomSecondaryColor.R),
                                new XElement("Green", v.CustomSecondaryColor.G),
                                new XElement("Blue", v.CustomSecondaryColor.B)))
                           );

        string[] modNames = Enum.GetNames(typeof(VehicleMod));
        XElement[] modElements = new XElement[modNames.Length];

        for (int i = 0; i < modElements.Length; i++) {
            modElements[i] = new XElement(modNames[i], v.GetMod(stringToMod(modNames[i])));
            vehicleElement.Add(modElements[i]);
        }

        savedVehicles_SpawnedDriven.Add(vehicleElement);
        xElement.Add(vehicleElement);
        xElement.Save(filePath);
        UI.Notify("Succesfully saved the vehicle!");
    }

    private void removeVehicles() {
        //foreach() {
          //  var.delete
        //}
    }

    private void reloadSavedVehicles() {
        removeVehicles();
        loadSavedVehicels(filePath);
    }


    void OnTick(object sender, EventArgs e) {
        UI.ShowSubtitle("Ticking...");
        //spawnVehiclesInProximity();
        // cleanUpActiveVehicles();
    }

    void OnKeyDown(object sender, KeyEventArgs e) {
    }

    void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.J)
        {
            if (Game.Player.Character.IsInVehicle())
            {
                saveVehicle(player.CurrentVehicle);
            } else
            {
                UI.Notify("Please get in a vehicle to be able to save it!");
            }
        }
        //DEBUGGING & TESTING
        if ( e.KeyCode == Keys.K) {

            UI.Notify("TEST");

            Vehicle v = World.CreateVehicle("Zentorno", (player.Position + player.ForwardVector * 5));
            v.PlaceOnGround();


            // Blips = Map markers ... 
            Blip b =  World.CreateBlip(player.Position);
            b.Scale = 0.6f;
            b.Name = v.DisplayName;
            b.Color = BlipColor.Green; // Attatch blip to vehicle !?

            /* Debugging :
            Vehicle v = World.CreateVehicle("Zentorno", (player.Position + player.ForwardVector * 5));
            UI.Notify("try and set mods");
            v.Heading = 90; // works
            v.CustomPrimaryColor = Color.Aqua; // works
            v.CustomSecondaryColor = Color.FromArgb(255, 0, 255, 0);
            //v.DirtLevel = 750f; // does not seem to work

            v.InstallModKit();

            UI.Notify("mod: " + player.CurrentVehicle.GetMod(VehicleMod.Spoilers));
            UI.Notify("spoieleroptions: " + player.CurrentVehicle.GetModCount(VehicleMod.Spoilers));

            v.CustomPrimaryColor = Color.Red;

            v.SetMod(VehicleMod.Spoilers, 6, false); // error

            // player.CurrentVehicle.SetMod(VehicleMod.Spoilers, 6, false); // works

            //v.SetMod(VehicleMod.Spoilers, 6, false); // error
            UI.Notify("L pressed");
            */
        }
        if (e.KeyCode == Keys.L) {
            reloadSavedVehicles();
        }
    }
}
