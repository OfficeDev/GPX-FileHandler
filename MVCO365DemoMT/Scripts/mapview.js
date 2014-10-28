function getMap() {
    window.map = new Microsoft.Maps.Map(document.getElementById("mapDiv"), { credentials: "Alk92IczOUAQdc0cg-R5hg5AASIgpG_thxDrP47lgANiAa027XsXBDTc_2hzPr_s" });
}

function pinLocation(lat, long) {
    var loc = new Microsoft.Maps.Location(lat, long);
    var pin = new Microsoft.Maps.Pushpin(loc);
    window.map.entities.push(pin);
    window.map.setView({ center: loc, zoom: 18 });
}

function init() {

    //hide scrollbar
    document.body.style.overflow = 'hidden';

    //load the map
    getMap();

    //iterate over hidden point list and plot points
    var locs = [];
    $(".hiddenCoordinateList").children().each(function () {
        var coordinate = $(this).text().split(",");
        locs.push(new Microsoft.Maps.Location(coordinate[0], coordinate[1]));
    });

    //plot points
    var pin;
    locs.forEach(function (loc) {
        pin = new Microsoft.Maps.Pushpin(loc);
        window.map.entities.push(pin);
    });
    
    //view all pins on the screen
    if (locs.length > 1) {
        var bestview = Microsoft.Maps.LocationRect.fromLocations(locs);
        console.log(bestview);
        map.setView({ bounds: bestview });
    } else if (locs.length) {
        console.log(locs[0]);
        window.map.setView({ center: locs[0], zoom: 18 });
    }
    
    //set it so that you can update the name
    $("#updateName").click(function (event) {
        var newName = $("#titleField").val().trim();
        if (newName) {
            $.get("/filehandler/save?newname=" + newName, function () {
            }).done(function () {
                //close the window if success
                $("#basicModal").modal("hide");
            }).fail(function (data, textStatus, xhr) {
                alert("Cannot save file");
            });
        }
    });

}

