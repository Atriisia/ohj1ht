using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;

public class ht : PhysicsGame
{
    private PlatformCharacter pelaaja;
    private PlatformCharacter vihollinen;
    private Cannon ase;
    private Timer restartDelay;

    private Image pelaajanKuva = LoadImage("pelaaja.png");
    private Image kruunuKuva = LoadImage("kruunu.png");
    private Image tahtiKuva = LoadImage("tahti.png");
    private Image esteKuva = LoadImage("este.png");
    private Image vihuKuva = LoadImage("vihu.png");
    private Image heittoTahtiKuva = LoadImage("heittotahti.png");
    private Timer taimeri = new Timer();
    private const double nopeus = 100;
    private const double hyppynopeus = 600;
    private double valiaika = 0;
    private int tahtienLkmKentassa = 0;
    private int nykyinenKentta = 0;
    private string[] kentat = new string[] { "kentta.txt", "kentta2.txt", "kentta3.txt" };

    public override void Begin()
    {
        //LataaKenttaUudelleen();
        //SetWindowSize(1920, 1080, true);
        SetWindowSize(800, 600, false);
        debug();
        mainMenu();
    }

    private void mainMenu()
    {
        MultiSelectWindow MainMenu = new MultiSelectWindow("Main menu", "Start game", "TOP 10", "Quit");
        MainMenu.AddItemHandler(0, UusiPeli);
        MainMenu.AddItemHandler(2, ConfirmExit);
        Add(MainMenu);
    }
    private void LuoTaimeri()
    {
        taimeri.CurrentTime = valiaika;
        taimeri.Start();

        //Widget suorakulmio = new Widget(30, 30, Shape.Rectangle);
        //suorakulmio.BorderColor = Color.Black;
        //suorakulmio.Position = new Vector((Window.ClientBounds.Width - suorakulmio.Width) / 2 - 20, (Window.ClientBounds.Height - suorakulmio.Height) / 2);
        //suorakulmio.Layer = Layer.CreateStaticLayer();
        Label taimeriNaytto = new Label();
        //                                  (1920 - 20) / 2 = 950
        //                                                              (1080 - 20) / 2 = 530
        taimeriNaytto.Position = new Vector((Window.ClientBounds.Width - taimeriNaytto.Width) / 2, -1*(Window.ClientBounds.Height - taimeriNaytto.Height) / 2);
        taimeriNaytto.TextColor = Color.Black;
        taimeriNaytto.DecimalPlaces = 2;
        taimeriNaytto.BindTo(taimeri.SecondCounter);
        Add(taimeriNaytto);

        
        //suorakulmio.Layer.RelativeTransition = 
        //Add(suorakulmio);
    }

    private void debug()
    {
        MessageDisplay.Add("tahtienlkm: " + tahtienLkmKentassa.ToString());
        MessageDisplay.Add("nykyinen kentta: " + nykyinenKentta.ToString());
    }

    private void SeuraavaKentta()
    {
        nykyinenKentta = nykyinenKentta + 1;
        LataaKenttaUudelleen();
    }

    private void LuoTaso(string kentta)
    {

        TileMap taso = TileMap.FromLevelAsset(kentta);
        taso.SetTileMethod('N', LisaaNinja);
        taso.SetTileMethod('k', LisaaKruunu);
        taso.SetTileMethod('v', LisaaVihollinen);
        taso.SetTileMethod('e', LisaaEste);
        taso.SetTileMethod('#', LisaaTaso);
        taso.SetTileMethod('*', LisaaTahti);
        taso.Execute(20, 20);
        Level.CreateBorders();
        Level.Background.CreateGradient(Color.Orange, Color.Red);
    }


    private void UusiPeli()
    {
        nykyinenKentta = 0;
        valiaika = 0;
        LataaKenttaUudelleen();
        luoAse();
        LuoTaimeri();
    }

    private void luoAse()
    {
        ase = new Cannon(30, 10);
        ase.InfiniteAmmo = true;
        ase.AttackSound = null;
        ase.ProjectileCollision = heittoTahtiTormasiVihuun;
        pelaaja.Add(ase);
        ase.IsVisible = false;
    }
    private void Heita(PlatformCharacter hahmo)
    {
        PhysicsObject heittoTahti = ase.Shoot();
        if (heittoTahti != null)
        {
            heittoTahti.Image = heittoTahtiKuva;
        }
    }
    private void LataaKenttaUudelleen()
    {
        tahtienLkmKentassa = 0;
        if (restartDelay != null)
        {
            restartDelay.Stop();
        }

        if (nykyinenKentta == 3)
        {
            Timer.CreateAndStart(5, UusiPeli);
            MessageDisplay.Add("Game Over!");
        }
        else
        {
            valiaika = taimeri.CurrentTime;
            ClearControls();
            ClearGameObjects();
            LuoTaimeri();
            LuoTaso(kentat[nykyinenKentta]);
            luoAse();
            LisaaNappaimet();
            Camera.StayInLevel = true;
            Camera.Follow(pelaaja);
            Camera.ZoomFactor = 1.2;
            Gravity = new Vector(0, -1500);
        }
    }
    private void LisaaNinja(Vector paikka, double leveys, double korkeus)
    {
        pelaaja = new PlatformCharacter(leveys, korkeus);
        pelaaja.Position = paikka;
        pelaaja.Image = pelaajanKuva;
        AddCollisionHandler(pelaaja, "tahti", PelaajaTormasiTahteen);
        AddCollisionHandler(pelaaja, "kruunu", PelaajaTormasiKruunuun);
        AddCollisionHandler(pelaaja, "este", PelaajaTormasiEsteeseen);
        Add(pelaaja);
    }

    private void LisaaHeittoTahti(Vector paikka, double leveys, double korkeus)
    {
        Cannon heittotahti = new Cannon(10, 10);
        heittotahti.Position = paikka;
        heittotahti.Image = heittoTahtiKuva;
        Add(heittotahti);

    }


    private void PelaajaTormasiTahteen(PhysicsObject pelaaja, PhysicsObject tahti)
    {
        tahti.Destroy();
        tahtienLkmKentassa--;
        debug();
    }

    private void PelaajaTormasiKruunuun(PhysicsObject pelaaja, PhysicsObject kruunu)
    {
        if (tahtienLkmKentassa == 0)
        {
            kruunu.Destroy();
            SeuraavaKentta();
        }
    }

    private void PelaajaTormasiEsteeseen(PhysicsObject pelaaja, PhysicsObject este)
    {
        pelaaja.Destroy();
        MessageDisplay.Add("Kuolit!");
        restartDelay = Timer.CreateAndStart(1, LataaKenttaUudelleen);
    }

    private void LisaaVihollinen(Vector paikka, double leveys, double korkeus)
    {
        vihollinen = new PlatformCharacter(leveys, korkeus);
        vihollinen.Position = paikka;
        vihollinen.Image = vihuKuva;
        vihollinen.Tag = "vihu";
        PlatformWandererBrain aivot = new PlatformWandererBrain();
        aivot.Speed = 20;
        vihollinen.Brain = aivot;
        Add(vihollinen);
    }

    private void heittoTahtiTormasiVihuun(PhysicsObject heittotahti, PhysicsObject vihu)
    {
        if (vihu.Tag.ToString() == "vihu")
        {
            heittotahti.Destroy();
            vihu.Destroy();
            MessageDisplay.Add("Tapoit vihun!");
        }
    }
    private void LisaaKruunu(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject kruunu = PhysicsObject.CreateStaticObject(leveys, korkeus);
        kruunu.IgnoresCollisionResponse = true;
        kruunu.Position = paikka;
        kruunu.Image = kruunuKuva;
        kruunu.Tag = "kruunu";
        Add(kruunu);
    }
    private void LisaaTahti(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject tahti = PhysicsObject.CreateStaticObject(leveys, korkeus);
        tahti.IgnoresCollisionResponse = true;
        tahti.Position = paikka;
        tahti.Image = tahtiKuva;
        tahti.Tag = "tahti";
        Add(tahti);
        tahtienLkmKentassa++;
        debug();
    }

    private void LisaaEste(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject este = PhysicsObject.CreateStaticObject(leveys, korkeus);
        este.IgnoresCollisionResponse = true;
        este.Position = paikka;
        este.Image = esteKuva;
        este.Tag = "este";
        Add(este);
    }

    private void LisaaTaso(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);
        taso.Position = paikka;
        taso.Color = Color.Gray;
        Add(taso);
    }

    private void LisaaNappaimet()
    {
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        Keyboard.Listen(Key.Left, ButtonState.Down, LiikutaVasemmalle, "Liikkuu vasemmalle", pelaaja, -nopeus);
        Keyboard.Listen(Key.Right, ButtonState.Down, Liikuta, "Liikkuu vasemmalle", pelaaja, nopeus);
        Keyboard.Listen(Key.Up, ButtonState.Pressed, Hyppaa, "Pelaaja hyppää", pelaaja, hyppynopeus);
        Keyboard.Listen(Key.X, ButtonState.Pressed, Heita, "Pelaaja heittää heittotähden", pelaaja);

        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        ControllerOne.Listen(Button.DPadLeft, ButtonState.Down, Liikuta, "Pelaaja liikkuu vasemmalle", pelaaja, -nopeus);
        ControllerOne.Listen(Button.DPadRight, ButtonState.Down, Liikuta, "Pelaaja liikkuu oikealle", pelaaja, -nopeus);
        ControllerOne.Listen(Button.A, ButtonState.Pressed, Hyppaa, "Pelaaja hyppää", pelaaja, hyppynopeus);

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
    }

    private void Liikuta(PlatformCharacter hahmo, double nopeus)
    {
        ase.Angle = Angle.FromDegrees(0);
        hahmo.Walk(nopeus);
    }

    private void LiikutaVasemmalle(PlatformCharacter hahmo, double nopeus)
    {
        ase.Angle = Angle.FromDegrees(180);
        hahmo.Walk(nopeus);
    }


    private void Hyppaa(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Jump(nopeus);
    }

}

