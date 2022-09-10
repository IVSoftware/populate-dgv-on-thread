If you would be open to a simpler approach than using `BackgroundWorker` what I would suggest is making an `async` handler for the `textbox.TextChanged` event that takes note of the keystroke count going before awaiting a "cooling off" period for rapid typing. If the count is the same before and after awaiting this delay it indicates that typing is sufficiently stable to perform a query.

![screenshot](https://github.com/IVSoftware/populate-dgv-on-thread/blob/master/populate_dgv_on_thread/Screenshots/screenshot.png)

I have mocked this out using `sqlit-net-pcl` for the sake of expediency. The DGV is bound to a `DataSource` that is a `BindingList<Game>`. The `OnLoad` override of `MainForm` initializes the `DataGridView`, then whatever database server you're using. The last thing is to subscribe to the `TextChanged` event and this is where all the where all the action takes place.


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
`TextBox.TextChanged` event handler (async)
```
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


