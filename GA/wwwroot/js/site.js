// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


//Ordenar caixas de seleção
 function selectSort (select, attr, order) {
    if (attr === 'text') {
        if (order === 'asc') {
            $(select).html($(select).children('option').sort(function (x, y) {
                return $(x).text().toUpperCase() < $(y).text().toUpperCase() ? -1 : 1;
            }));
            $(select).get(0).selectedIndex = 0;
        }// end asc
        if (order === 'desc') {
            $(select).html($(select).children('option').sort(function (y, x) {
                return $(x).text().toUpperCase() < $(y).text().toUpperCase() ? -1 : 1;
            }));
            $(select).get(0).selectedIndex = 0;
        }// end desc
     }
}


function goBack() {
    window.history.back();
}


function SelectAllItens(lista) {
    $("#" + lista + " option").prop("selected", true);
}

function AddItemTextSelect(origem, destino) {
    var folder = document.getElementById(origem);
    var folders = document.getElementById(destino);
    opt = new Option(folder.value, folder.value);
    folders.options.add(opt);
    folder.value = "";
}

function RemoveItemList(lista) {
    var s = 1;
    var Index;
    var folders = document.getElementById(lista);

    while (s > 0) {
        Index = folders.selectedIndex;
        if (Index >= 0) {
            folders.options[Index] = null;
        }
        else
            s = 0;
    }
    return true;
}


