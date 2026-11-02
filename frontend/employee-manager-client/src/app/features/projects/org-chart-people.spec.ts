import { OrgChartNode } from '../org-chart/org-chart.model';
import { directReportsOf, flattenOrgChart } from './org-chart-people';

function node(id: string, directReports: OrgChartNode[] = []): OrgChartNode {
  return { id, firstName: id, lastName: 'X', jobTitle: '', email: '', directReports };
}

const chart = [node('ceo', [node('lead', [node('dev1'), node('dev2')]), node('assistant')])];

describe('flattenOrgChart', () => {
  it('lists everyone with their manager', () => {
    expect(flattenOrgChart(chart).map((p) => [p.id, p.managerId])).toEqual([
      ['ceo', null],
      ['lead', 'ceo'],
      ['dev1', 'lead'],
      ['dev2', 'lead'],
      ['assistant', 'ceo'],
    ]);
  });
});

describe('directReportsOf', () => {
  it('finds the reports of someone deep in the chart', () => {
    expect(directReportsOf(chart, 'lead').map((p) => p.id)).toEqual(['dev1', 'dev2']);
  });

  it('is empty for someone with no reports or not in the chart', () => {
    expect(directReportsOf(chart, 'dev1')).toEqual([]);
    expect(directReportsOf(chart, 'nobody')).toEqual([]);
  });
});
