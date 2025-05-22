using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Gra2D
{
    public partial class MainWindow : Window
    {
        // Stałe reprezentujące rodzaje terenu
        public const int LAS = 1;     // las
        public const int LAKA = 2;     // łąka
        public const int SKALA = 3;   // skały
        public const int ZLOTO = 4;   //złoto
        public const int ILE_TERENOW = 5;   // ile terenów
        // Mapa przechowywana jako tablica dwuwymiarowa int
        private int[,] mapa;
        private int szerokoscMapy;
        private int wysokoscMapy;
        // Dwuwymiarowa tablica kontrolek Image reprezentujących segmenty mapy
        private Image[,] tablicaTerenu;
        // Rozmiar jednego segmentu mapy w pikselach
        private const int RozmiarSegmentu = 32;

        // Tablica obrazków terenu – indeks odpowiada rodzajowi terenu
        // Indeks 1: las, 2: łąka, 3: skały
        private BitmapImage[] obrazyTerenu = new BitmapImage[ILE_TERENOW];

        // Pozycja gracza na mapie
        private int pozycjaGraczaX = 0;
        private int pozycjaGraczaY = 0;
        // Obrazek reprezentujący gracza
        private Image obrazGracza;
        // Licznik zgromadzonego drewna
        private int iloscDrewna = 0;
        private int iloscZlota = 0;
        private int iloscSkal = 0;
        private List<Misja> misje = new List<Misja>();
        private int indeksAktualnejMisji = 0;
        
        public MainWindow()
        {
            InitializeComponent();
            WczytajObrazyTerenu();

            MenuGlowne.Visibility = Visibility.Visible;
            SiatkaMapy.Visibility = Visibility.Collapsed;
            // Inicjalizacja obrazka gracza
            obrazGracza = new Image
            {
                Width = RozmiarSegmentu,
                Height = RozmiarSegmentu
            };
            BitmapImage bmpGracza = new BitmapImage(new Uri("gracz.png", UriKind.Relative));
            obrazGracza.Source = bmpGracza;

            misje.Add(new Misja { Nazwa = "Zbuduj młot", Drewno = 3, Skala = 0, Zloto = 0, Nagroda = "Kilof" });
            misje.Add(new Misja { Nazwa = "Zbierz skały", Drewno = 0, Skala = 5, Zloto = 0, Nagroda = "Mozesz zebrac zloto" });
            PokazAktualnaMisje();

        }
        private void WczytajObrazyTerenu()
        {
            // Zakładamy, że tablica jest indeksowana od 0, ale używamy indeksów 1-3
            obrazyTerenu[LAS] = new BitmapImage(new Uri("las.png", UriKind.Relative));
            obrazyTerenu[LAKA] = new BitmapImage(new Uri("laka.png", UriKind.Relative));
            obrazyTerenu[SKALA] = new BitmapImage(new Uri("skala.png", UriKind.Relative));
            obrazyTerenu[ZLOTO] = new BitmapImage(new Uri("zloto.png", UriKind.Relative));
        }

        // Wczytuje mapę z pliku tekstowego i dynamicznie tworzy tablicę kontrolek Image
        private void WczytajMape(string sciezkaPliku)
        {
            try
            {
                var linie = File.ReadAllLines(sciezkaPliku);//zwraca tablicę stringów, np. linie[0] to pierwsza linia pliku
                wysokoscMapy = linie.Length;
                szerokoscMapy = linie[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;//zwraca liczbę elementów w tablicy
                mapa = new int[wysokoscMapy, szerokoscMapy];

                for (int y = 0; y < wysokoscMapy; y++)
                {
                    var czesci = linie[y].Split(' ', StringSplitOptions.RemoveEmptyEntries);//zwraca tablicę stringów np. czesci[0] to pierwszy element linii
                    for (int x = 0; x < szerokoscMapy; x++)
                    {
                        mapa[y, x] = int.Parse(czesci[x]);//wczytanie mapy z pliku
                    }
                }

                // Przygotowanie kontenera SiatkaMapy – czyszczenie elementów i definicji wierszy/kolumn
                SiatkaMapy.Children.Clear();
                SiatkaMapy.RowDefinitions.Clear();
                SiatkaMapy.ColumnDefinitions.Clear();

                for (int y = 0; y < wysokoscMapy; y++)
                {
                    SiatkaMapy.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(RozmiarSegmentu) });
                }
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    SiatkaMapy.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(RozmiarSegmentu) });
                }

                // Tworzenie tablicy kontrolk Image i dodawanie ich do siatki
                tablicaTerenu = new Image[wysokoscMapy, szerokoscMapy];
                for (int y = 0; y < wysokoscMapy; y++)
                {
                    for (int x = 0; x < szerokoscMapy; x++)
                    {
                        Image obraz = new Image
                        {
                            Width = RozmiarSegmentu,
                            Height = RozmiarSegmentu
                        };

                        int rodzaj = mapa[y, x];
                        if (rodzaj >= 1 && rodzaj < ILE_TERENOW)
                        {
                            obraz.Source = obrazyTerenu[rodzaj];//wczytanie obrazka terenu
                        }
                        else
                        {
                            obraz.Source = null;
                        }

                        Grid.SetRow(obraz, y);
                        Grid.SetColumn(obraz, x);
                        SiatkaMapy.Children.Add(obraz);//dodanie obrazka do siatki na ekranie
                        tablicaTerenu[y, x] = obraz;
                    }
                }

                // Dodanie obrazka gracza – ustawiamy go na wierzchu
                SiatkaMapy.Children.Add(obrazGracza);
                Panel.SetZIndex(obrazGracza, 1);//ustawienie obrazka gracza na wierzchu
                pozycjaGraczaX = 0;
                pozycjaGraczaY = 0;
                AktualizujPozycjeGracza();

                iloscDrewna = 0;
                EtykietaDrewna.Content = "Drewno: " + iloscDrewna;
            }//koniec try
            catch (Exception ex)
            {
                MessageBox.Show("Błąd wczytywania mapy: " + ex.Message);
            }
        }

        // Aktualizuje pozycję obrazka gracza w siatce
        private void AktualizujPozycjeGracza()
        {
            Grid.SetRow(obrazGracza, pozycjaGraczaY);
            Grid.SetColumn(obrazGracza, pozycjaGraczaX);
        }

        // Obsługa naciśnięć klawiszy – ruch gracza oraz wycinanie lasu
        private void OknoGlowne_KeyDown(object sender, KeyEventArgs e)
        {
            int nowyX = pozycjaGraczaX;
            int nowyY = pozycjaGraczaY;
            //zmiana pozycji gracza
            if (e.Key == Key.Up) nowyY--;
            else if (e.Key == Key.Down) nowyY++;
            else if (e.Key == Key.Left) nowyX--;
            else if (e.Key == Key.Right) nowyX++;
            //Gracz nie może wyjść poza mapę
            if (nowyX >= 0 && nowyX < szerokoscMapy && nowyY >= 0 && nowyY < wysokoscMapy)

            // Gracz nie może wejść na pole ze skałami
            {

                if (mapa[nowyY, nowyX] != SKALA)
                {
                    pozycjaGraczaX = nowyX;
                    pozycjaGraczaY = nowyY;
                    AktualizujPozycjeGracza();
                }
            }
        

            // Obsługa wycinania lasu – naciskamy klawisz C
            if (e.Key == Key.C)
            {
                int typTerenu = mapa[pozycjaGraczaY, pozycjaGraczaX];

                switch (typTerenu)
                {
                    case LAS:
                        mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                        tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                        iloscDrewna++;
                        break;
                    case ZLOTO:
                        
                        mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                        tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                        iloscZlota++;
                        break;

                    case SKALA:
                        mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                        tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[ZLOTO];
                        iloscSkal++;
                        break;
                }
            }
            if (e.Key == Key.N)
            {
                int typTerenu = mapa[pozycjaGraczaY, pozycjaGraczaX];

                if (typTerenu == ZLOTO)
                {
                    mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                    tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                    iloscZlota++;
                }
                else if (typTerenu == SKALA)
                {
                    mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                    tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                    iloscSkal--;
                }
                
            }

            EtykietaDrewna.Content = $"drewno:  {iloscDrewna}Skała:{iloscSkal} Złoto: {iloscZlota}";
        }

        // Obsługa przycisku "Wczytaj mapę"
        private void WczytajMape_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog oknoDialogowe = new OpenFileDialog();
            oknoDialogowe.Filter = "Plik mapy (*.txt)|*.txt";
            oknoDialogowe.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory; // Ustawienie katalogu początkowego
            bool? czyOtwartoMape = oknoDialogowe.ShowDialog();
            if (czyOtwartoMape == true)
            {
                WczytajMape(oknoDialogowe.FileName);
            }
            iloscZlota = 0;
            iloscDrewna = 0;
            iloscSkal = 0;


            EtykietaDrewna.Content = $"Drewno:{iloscDrewna} skała:{iloscSkal} Złoto:{iloscZlota}";
        }

        /*private void ListaSkinow_SelectionChanged(object sender, SelectionChangedEventArgs e)//metoda na zmiane skinow
        {
            if (Skiny.SelectedItem is ComboBoxItem wybranySkin && wybranySkin.Tag !=null)
            {
                string nazwaPliku = wybranySkin.Tag.ToString();

                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(nazwaPliku, UriKind.Relative);
                    bitmap.EndInit();
                   
                   // obrazGracza.Source = bitmap;

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Nie udało się załadować skina:    {ex.Message}");
                }
            }
        }*/
        public class Misja
        {
            public string Nazwa { get; set; }
            public int Drewno {  get; set; }
            public int Skala { get; set; }
            public int Zloto {  get; set; }
            public string Nagroda { get; set; }
            public bool Ukonczona {  get; set; }

            public bool CzyUkonczona(int drewno, int skala, int zloto)
            {
                return drewno >= Drewno && skala >= Skala && zloto >= Zloto;
            }
        }
        private void PokazAktualnaMisje()
        {
            if (indeksAktualnejMisji < misje.Count)
            {
                var m = misje[indeksAktualnejMisji];
                EtykietaMisji.Content = $"Misja:    {m.Nazwa} - potrzebne:  {m.Drewno} drewna, {m.Skala} skał, {m.Zloto} złota";
            }
            else
            {
                EtykietaMisji.Content = "wszystkie misje wykonane!";
            }

            
        }

        private void crafting_Click(object sender, RoutedEventArgs e)
        {
            if (indeksAktualnejMisji < misje.Count)
            {
                var m = misje[indeksAktualnejMisji];
                if (!m.Ukonczona && m.CzyUkonczona(iloscDrewna, iloscSkal, iloscZlota))
                {
                    iloscDrewna -= m.Drewno;
                    iloscSkal -= m.Skala;
                    iloscZlota -= m.Zloto;


                    m.Ukonczona = true;
                    MessageBox.Show($"Misja ukończona! Otrzymano: {m.Nagroda}");

                    indeksAktualnejMisji++;
                   

                    EtykietaDrewna.Content = $"Drewno: {iloscDrewna} Skały: {iloscSkal} Złoto:  {iloscZlota}";
                }
                else
                {
                    MessageBox.Show("Nie masz wystarczających surowców.");
                }
            }
        }

        private void Zacznij_Click(object sender, RoutedEventArgs e)
        {
            MenuGlowne.Visibility = Visibility.Collapsed;
            SiatkaMapy.Visibility = Visibility.Visible;
        }

        private void Skin1_Click(object sender, RoutedEventArgs e)
        {
            BitmapImage bmpGracza = new BitmapImage(new Uri("gracz.png", UriKind.Relative));
            obrazGracza.Source = bmpGracza;
        }

        private void Skin2_Click(object sender, RoutedEventArgs e)
        {
            BitmapImage bmpGracza = new BitmapImage(new Uri("gracz2.png", UriKind.Relative));
            obrazGracza.Source = bmpGracza;
        }

        private void Skin3_Click(object sender, RoutedEventArgs e)
        {
            BitmapImage bmpGracza = new BitmapImage(new Uri("gracz3.png", UriKind.Relative));
            obrazGracza.Source = bmpGracza;
        }
    }
}


