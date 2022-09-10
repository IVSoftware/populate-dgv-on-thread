using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace populate_dgv_on_thread
{
    public partial class MainForm : Form
    {
        string ConnectionString =>
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "GamesDatabase");
        public MainForm()
        {
            InitializeComponent();
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            initDGV();
            initSQL();
            textBox.TextChanged += onTextBoxTextChanged;
        }
        BindingList<Game> DataSource = new BindingList<Game>();
        // Comparing the awaited count to the total count so that
        // the DGV isn't visually updated until all queries have run.
        int _queryCount = 0;

        private async void onTextBoxTextChanged(object sender, EventArgs e)
        {
            if(string.IsNullOrWhiteSpace(textBox.Text))
            {
                return;
            }
            _queryCount++;
            var queryCountB4 = _queryCount;
            List<Game> recordset = null;
            var captureText = textBox.Text;

            // Settling time for rapid typing to cease.
            await Task.Delay(TimeSpan.FromMilliseconds(250));

            // If keypresses occur in rapid succession, only
            // respond to the latest one after a settline timeout.
            if (_queryCount.Equals(queryCountB4))
            {
                await Task.Run(() =>
                {
                    using (var cnx = new SQLiteConnection(ConnectionString))
                    {
                        var sql = $"SELECT * FROM games WHERE GameID LIKE '{captureText}%'";
                        recordset = cnx.Query<Game>(sql);
                    }
                });
                DataSource.Clear();
                foreach (var game in recordset)
                {
                    DataSource.Add(game);
                }
            }
            else
            {
                Debug.WriteLine("Waiting for all pending queries to complete");
            }
        }

        private void initDGV()
        {
            dataGridView.DataSource = DataSource;
            dataGridView.AllowUserToAddRows = false;
            DataSource.Add(new Game { GameID = "Generate Columns" });
            dataGridView
                .Columns[nameof(Game.GameID)]
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView
                .Columns[nameof(Game.Created)]
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            DataSource.Clear();
        }

        private void initSQL()
        {
            // For testing, start from scratch every time
            if (File.Exists(ConnectionString)) File.Delete(ConnectionString);
            using (var cnx = new SQLiteConnection(ConnectionString))
            {
                cnx.CreateTable<Game>();
                cnx.Insert(new Game { GameID = "Awe" });
                cnx.Insert(new Game { GameID = "Abilities" });
                cnx.Insert(new Game { GameID = "Abscond" });
                cnx.Insert(new Game { GameID = "Absolve" });
                cnx.Insert(new Game { GameID = "Absolute" });
            }
        }
    }
    [Table("games")]
    class Game
    {
        [PrimaryKey, Browsable(false)]
        public string Guid { get; set; } = System.Guid.NewGuid().ToString().ToUpper();
        public string GameID { get; set; }
        private DateTime _created = DateTime.Now.Date;
        public string Created 
        {
            get => _created.ToShortDateString();
            set
            {
                if(DateTime.TryParse(value, out DateTime dt))
                {
                    _created = dt.Date;
                }
            }
        }
    }
}
