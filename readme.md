What I would suggest is making a Task whenever the `textbox.Text` changes that will execute a new query on the database, but then only update the `DataGridView` if we're sure all of the pending queries have run. I have mocked this out using `sqlit-net-pcl` for the sake of expediency.



