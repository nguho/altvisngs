# altvisngs
Alternative visualization of next-gen sequencing data
V1.0.0

Nick M. Guho

2016 JUN 15

*Note: This file is a work in progress.*

*Description*

`altvisngs` is a console program written in C# which takes the output from the [`dbcAmplicons`](https://github.com/msettles/dbcAmplicons) pipeline (or any other which can output the requisite data files) and creates various figures (heat maps, summary bar plots, hierarchical dendograms, and taxonomic hierarchy bar plots) and tables using `LaTeX`. The program was created as a means to automate the generation of these routine components while also permiting the creation of the novel taxonomic hierarchy bar plots.

*Input*

Four files are required: `table.abundance.txt`, `table.proportions.txt`, `table.taxa_info.txt`, and `_key.csv`. All are ASCII text files. The first three are tab-delimited, while `_key.csv` is comma-delimited. 

`table.abundance.txt` contains the abundance (i.e., number of sequences) for the phylotypes (rows) and the samples (columns). The first row is the headings. The first column is the taxon name for the best classification of the phylotype (heading = "Taxon_Name"). The second column is the level of the best classification (heading = "Level"; recognized values are "domain", "phylum", "class", "order", "family", and "genus"). Columns thereafter contain the abundance for each of the samples (heading = the unique sample identification).

`table.proportions.txt` is the same format as `table.abundance.txt` except that the relative abundance (i.e., abundance divided by total number of sequences in the sample) is reported.

`table.taxa_info.txt` may contain any number of columns, but the first must be headed with "Taxon_Name" with the rows corresponding to the full taxonomic classification up to the best classification in `table.abundance.txt` and `table.proportions.txt`. These are semi-colon delimited and prefixed by an abbreviated taxonomic level (i.e., domain = "d__", phylum="p__", class="c__", order="o__", family="f__", genus="g__"). An example entry:

d__Bacteria;p__Acidobacteria;c__Acidobacteria_Gp1;o__Acidobacterium;f__Acidobacterium;g__Acidobacterium

Again note that the classification stops at the best classification.

`_key.csv` is the comma separated file that translates from the unique sample identification string to other relevant attributes for the sample. These may be any number of values, but the first column must correspond to the unique sample identificaiton strings (heading = "Name"). `altvisngs` permits samples to be grouped by an attribute (e.g., the reactor from which the sample was taken) which are differentiated by a subgroup attribute (e.g., the date on which the sample was taken from the reactor). Note that the subgroup is indended to be unique for the group (i.e., for the example, no two samples should be taken on the same date for a given reactor).

*File Structure*

The program is indended to permit the evaluation of mulitple primer sets. Each primer set has the four files noted above. Therefore, to evaluate multiple primer sets, the following file structure is assumed:

`Parent Directory`<br>
` |--EUB`<br>
` |  |--_key.csv`<br>
` |  |--table.abundance.txt`<br>
` |  |--table.proportions.txt`<br>
` |  |--table.taxa_info.txt`<br>
` |`<br>
` |--PAO`<br>
` ` ` ` `|--_key.csv`<br>
`    |--table.abundance.txt`<br>
`    |--table.proportions.txt`<br>
`    |--table.taxa_info.txt`<br>

where `EUB` and `PAO` are the names of the primer sets.

*Quick Useage*

All of the possible `altvisngs` outputs may be generated calling (using the group, subgroup, and primers from the examples above):

`altvisngs -sub=all -group_attr=Reactor -groups=S-EBPR G-EBPR -subgroup_attr=Date -primers=EUB PAO`

where `Reactor` is the heading in the `_key.csv` file corresponding to the group, `S-EBPR` and `G-EBPR` are instances of that group (i.e., reactors), `Date` is the heading in the `_key.csv` file corresponding to the subgroup, and `EUB` and `PAO` are directories in the working directory containing the four files as noted above.

*Dependencies*

`altvisngs` requires `.Net 4.0` or equivalent and a working `TeX` installation. `latex` and `pdflatex` are called automatically by numerous subroutines. Additionally, the `tikz`, `pgfplots`, `fp`, `standalone`, `helvet`, `fontenc`, `sansmath` packages/classes are required. The output assumes the `caption`, `siunitx`, `adjustbox` (with the `export` option), `cleveref`, `hyperref`, `threeparttablex`,  `booktabs`, `longtable` are loaded.
