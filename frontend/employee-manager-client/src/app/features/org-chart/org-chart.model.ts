export interface OrgChartNode {
  id: string;
  firstName: string;
  lastName: string;
  jobTitle: string;
  email: string;
  directReports: OrgChartNode[];
}
