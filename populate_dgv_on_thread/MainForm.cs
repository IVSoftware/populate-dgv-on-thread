using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SQLite;

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
            textBox.TextChanged += async (sender, e) =>
            {
                _queryCount++;
                var queryCountB4 = _queryCount;
                List<Game> recordset = null;
                var captureText = textBox.Text;
                await Task.Run(() =>
                {
                    using (var cnx = new SQLiteConnection(ConnectionString))
                    {
#if TEST_QUERY_REENTRY
                        Thread.Sleep(1000);
#endif
                        var sql = $"SELECT * FROM games WHERE GameID LIKE '{captureText}%'";
                        recordset = cnx.Query<Game>(sql);
                    }
                });

                // For efficient updates, only respond to the latest query.
                if (_queryCount.Equals(queryCountB4))
                {
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
            };
        }
        BindingList<Game> DataSource = new BindingList<Game>();

        // Comparing the awaited count to the total count so that
        // the DGV isn't visually updated until all queries have run.
        int _queryCount = 0;
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
