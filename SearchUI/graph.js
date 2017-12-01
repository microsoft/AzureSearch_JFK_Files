
var w = 1200;
var h = 1000;
var linkDistance = 150;

var colors = d3.scale.category10();

var dataset = {};

function LoadFDGraph() {
    $("#fdGraph").html('');
    var svg = d3.select("#fdGraph").append("svg").attr({ "width": w, "height": h });

    var force = d3.layout.force()
        .nodes(dataset.nodes)
        .links(dataset.edges)
        .size([w, h])
        .linkDistance([linkDistance])
        .charge([-1200])
        .theta(0.8)
        .gravity(0.05)
        .start();

    var edges = svg.selectAll("line")
        .data(dataset.edges)
        .enter()
        .append("line")
        .attr("id", function (d, i) { return 'edge' + i })
        .attr('marker-end', 'url(#arrowhead)')
        .style("stroke", "#ccc")
        .style("pointer-events", "none");

    var nodes = svg.selectAll("circle")
        .data(dataset.nodes)
        .enter()
        .append("circle")
        .attr({ "r": 15 })
        .style("fill", function (d, i) {
            if (i == 0)
                return "#FF3900";
            else
                return "#9ECAE1";
        })	// change "#9ECAE1" to colors(i) to get different colors
        .call(force.drag)

    nodes.on("dblclick.zoom", function (d) {
        var newTerm = d.name;

        // add the second degree connection term if there is no direct connection to the root.
        var connections = dataset.edges.filter(function (e) { return e.target == d && e.source.index == 0 });
        if (connections.length == 0) {
            connections = dataset.edges.filter(function (e) {
                return e.target == d && dataset.edges.filter(function (e2) {
                    return e2.target == e && e2.source.index == 0
                })
            });
            if (connections.length > 0)
                newTerm = connections[0].source.name + " " + newTerm;
        }


        var currentSearch = automagic.store.getState().parameters.input;
        automagic.store.setInput(currentSearch + " " + newTerm);
        setGraphVisible(false);
        automagic.store.search();

    });

    var nodelabels = svg.selectAll(".nodelabel")
        .data(dataset.nodes)
        .enter()
        .append("text")
        .attr({
            "x": function (d) { return d.x; },
            "y": function (d) { return d.y; },
            "class": "nodelabel",
            "stroke": "black"
        })
        .text(function (d) { return d.name; });

    var edgepaths = svg.selectAll(".edgepath")
        .data(dataset.edges)
        .enter()
        .append('path')
        .attr({
            'd': function (d) { return 'M ' + d.source.x + ' ' + d.source.y + ' L ' + d.target.x + ' ' + d.target.y },
            'class': 'edgepath',
            'fill-opacity': 0,
            'stroke-opacity': 0,
            'fill': 'blue',
            'stroke': 'red',
            'id': function (d, i) { return 'edgepath' + i }
        })
        .style("pointer-events", "none");

    svg.append('defs').append('marker')
        .attr({
            'id': 'arrowhead',
            'viewBox': '-0 -5 10 10',
            'refX': 25,
            'refY': 0,
            'orient': 'auto',
            'markerWidth': 10,
            'markerHeight': 10,
            'xoverflow': 'visible'
        })
        .append('svg:path')
        .attr('d', 'M 0,-5 L 10 ,0 L 0,5')
        .attr('fill', '#ccc')
        .attr('stroke', '#ccc');


    force.on("tick", function () {

        edges.attr({
            "x1": function (d) { return d.source.x; },
            "y1": function (d) { return d.source.y; },
            "x2": function (d) { return d.target.x; },
            "y2": function (d) { return d.target.y; }
        });

        nodes.attr({
            "cx": function (d) { return d.x; },
            "cy": function (d) { return d.y; }
        });

        nodelabels.attr("x", function (d) { return d.x; })
            .attr("y", function (d) { return d.y; });

        edgepaths.attr('d', function (d) {
            var path = 'M ' + d.source.x + ' ' + d.source.y + ' L ' + d.target.x + ' ' + d.target.y;
            //console.log(d)
            return path
        });

    });
};

var graphQuery;

function getFDNodes(q) {
    //get all the nodes for the force directed graph
    $("#fdGraph").html('Loading graph, please wait...');
    graphQuery = q;

    $.get('/api/data/GetFDNodes',
        {
            q: q
        },
        function (data) {
            dataset = data;
            LoadFDGraph();
        });
}






