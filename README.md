# Esstatic

A command line utility to quickly view EventStore stats files in spreadsheets.

When run, esstatic reads stats files, produces a table of data, and stores it in the clip board. Paste into one of the provided
spreadsheets for instant graphs.

# Features

- samples 200 stats entries across the collection of matching stats files
- select which stats files using the include/exclude glob patterns
- use test mode to output which files match the glob patterns
- use json paths to query the stats entries. aggregate and format.
- skip/take percent entries from the stats files for quick range selection

# TODO

- be able to specify a particular json path on the command line
- be able to specify a different pre-defined set of json paths on the command line
- more predefined json paths
- add license
- file output?
- ...
