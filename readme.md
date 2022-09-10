What I would suggest is making a Task whenever the `textbox.Text` changes that will execute a new query on the database, but then only update the `DataGridView` if we're sure all of the pending queries have run. I have mocked this out using `sqlit-net-pcl` for the sake of expediency. The DGV is bound to a `DataSource` that is a `BindingList<Game>`. The `OnLoad` override of `MainForm` initializes the `DataGridView`, then whatever database server you're using. The last thing is to subscribe to the `TextChanged` event and this is where all the where all the action takes place.

```        
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
```

***
TextChanged event handler (async)
```
private async void onTextBoxTextChanged(object sender, EventArgs e)
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
}
```

***
**DataGridView**
```
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
```

***
**Database**
```
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
```


