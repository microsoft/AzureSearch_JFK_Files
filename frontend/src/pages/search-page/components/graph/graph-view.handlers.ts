import * as d3 from "d3";
import { GraphNode } from "../../../../graph-api";


export const createDragBehaviour = (simulation) => {
  return d3.drag<SVGCircleElement, GraphNode>()
    .on("start", dragstarted(simulation))
    .on("drag", dragged)
    .on("end", dragended(simulation));
}

const dragstarted = (simulation) => (d) => {
  if (!d3.event.active) simulation.alphaTarget(1).restart();
  d.fx = d.x;
  d.fy = d.y;
}

const dragged = (d) => {
  d.fx = d3.event.x;
  d.fy = d3.event.y;
}

const dragended = (simulation) => (d) => {
  if (!d3.event.active) simulation.alphaTarget(0);
  d.fx = null;
  d.fy = null;
}
