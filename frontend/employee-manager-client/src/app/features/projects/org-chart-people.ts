import { TeamMember } from '../../shared/team-lines';
import { OrgChartNode } from '../org-chart/org-chart.model';

/** Everyone in the org chart, with their manager, for `teamLines`. */
export function flattenOrgChart(
  nodes: readonly OrgChartNode[],
  managerId: string | null = null,
): TeamMember[] {
  return nodes.flatMap((node) => [
    { id: node.id, managerId, firstName: node.firstName, lastName: node.lastName },
    ...flattenOrgChart(node.directReports, node.id),
  ]);
}

/** The direct reports of `id` (empty when they aren't in the chart), in org-chart order. */
export function directReportsOf(nodes: readonly OrgChartNode[], id: string): OrgChartNode[] {
  for (const node of nodes) {
    if (node.id === id) {
      return node.directReports;
    }
    const reports = directReportsOf(node.directReports, id);
    if (reports.length) {
      return reports;
    }
  }
  return [];
}
