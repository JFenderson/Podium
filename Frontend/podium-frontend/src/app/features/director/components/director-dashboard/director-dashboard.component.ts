// director-dashboard.component.ts
// Frontend/podium-frontend/src/app/features/director/components/director-dashboard/director-dashboard.component.ts

import { Component, OnInit, OnDestroy, inject, signal, computed, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Subject, interval } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { DirectorDashboardService } from '../../services/director-dashboard.service';
import {
  DirectorDashboardDto,
  DirectorKeyMetrics,
  DirectorDashboardFilters,
  FunnelStageDto,
  StaffPerformanceDto,
  PendingApprovalDto,
  ActivityItemDto,
  DateRange,
  PREDEFINED_DATE_RANGES,
  FUNNEL_STAGE_COLORS,
  ACTIVITY_CONFIG,
  ExportOptions
} from '../../../../core/models/director-dashboard.models';

// Register Chart.js components
Chart.register(...registerables);

@Component({
  selector: 'app-director-dashboard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './director-dashboard.component.html',
  styleUrls: ['./director-dashboard.component.scss']
})
export class DirectorDashboardComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private dashboardService = inject(DirectorDashboardService);
  private destroy$ = new Subject<void>();
  Math = Math; // For template usage
  // Chart references
  @ViewChild('funnelChart') funnelChartRef?: ElementRef<HTMLCanvasElement>;
  @ViewChild('offersTimeChart') offersTimeChartRef?: ElementRef<HTMLCanvasElement>;
  @ViewChild('offersBreakdownChart') offersBreakdownChartRef?: ElementRef<HTMLCanvasElement>;
  
  private funnelChart?: Chart;
  private offersTimeChart?: Chart;
  private offersBreakdownChart?: Chart;

  // State
  isLoading = signal(false);
  isRefreshing = signal(false);
  
  // Dashboard Data
  keyMetrics = signal<DirectorKeyMetrics | null>(null);
  recruitmentFunnel = signal<FunnelStageDto[]>([]);
  offersOverview = signal<any>(null);
  staffPerformance = signal<StaffPerformanceDto[]>([]);
  pendingApprovals = signal<PendingApprovalDto[]>([]);
  recentActivity = signal<ActivityItemDto[]>([]);
  
  // Filters
  filterForm!: FormGroup;
  selectedDateRange = signal<DateRange>(PREDEFINED_DATE_RANGES[1]); // Last 30 days
  readonly predefinedRanges = PREDEFINED_DATE_RANGES;
  
  // UI State
  showDatePicker = signal(false);
  showExportModal = signal(false);
  selectedFunnelStage = signal<string | null>(null);
  
  // Staff table sorting
  staffSortColumn = signal<string>('acceptanceRate');
  staffSortDirection = signal<'asc' | 'desc'>('desc');
  
  // Computed
  sortedStaff = computed(() => {
    const staff = [...this.staffPerformance()];
    const col = this.staffSortColumn();
    const dir = this.staffSortDirection();
    
    return staff.sort((a: any, b: any) => {
      const aVal = a[col] ?? 0;
      const bVal = b[col] ?? 0;
      return dir === 'asc' ? aVal - bVal : bVal - aVal;
    });
  });
  
  urgentApprovalsCount = computed(() => 
    this.pendingApprovals().filter(a => a.urgency === 'High').length
  );
  
  activeRecruitersCount = computed(() => 
    this.staffPerformance().filter(s => s.daysActive <= 7).length
  );
  
  // Constants for templates
  readonly FUNNEL_COLORS = FUNNEL_STAGE_COLORS;
  readonly ACTIVITY_CONFIG = ACTIVITY_CONFIG;
  
  // Auto-refresh interval (5 minutes)
  private refreshInterval = 5 * 60 * 1000;

  ngOnInit(): void {
    this.initFilterForm();
    this.loadDashboard();
    this.setupRealTimeUpdates();
    this.setupAutoRefresh();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.dashboardService.stopSignalR();
    this.destroyCharts();
  }

  private initFilterForm(): void {
    const range = this.selectedDateRange();
    
    this.filterForm = this.fb.group({
      startDate: [range.start],
      endDate: [range.end],
      recruiterId: [null],
      instrument: [''],
      offerStatus: ['']
    });
  }

  // ============================================
  // DATA LOADING
  // ============================================

  loadDashboard(): void {
    this.isLoading.set(true);
    
    const filters: DirectorDashboardFilters = {
      dateRangeStart: this.filterForm.value.startDate,
      dateRangeEnd: this.filterForm.value.endDate,
      recruiterId: this.filterForm.value.recruiterId,
      instrument: this.filterForm.value.instrument,
      offerStatus: this.filterForm.value.offerStatus
    };

    this.dashboardService.getDashboard(filters).subscribe({
      next: (data) => {
        this.keyMetrics.set(data.keyMetrics);
        this.recruitmentFunnel.set(data.recruitmentFunnel);
        this.offersOverview.set(data.offersOverview);
        this.staffPerformance.set(data.staffPerformance);
        this.pendingApprovals.set(data.pendingApprovals);
        this.recentActivity.set(data.recentActivity);
        
        this.isLoading.set(false);
        
        // Render charts after data loads
        setTimeout(() => this.renderCharts(), 100);
      },
      error: (error) => {
        console.error('Dashboard load error:', error);
        this.isLoading.set(false);
      }
    });
  }

  refreshDashboard(): void {
    this.isRefreshing.set(true);
    
    const filters: DirectorDashboardFilters = {
      dateRangeStart: this.filterForm.value.startDate,
      dateRangeEnd: this.filterForm.value.endDate
    };

    this.dashboardService.getDashboard(filters).subscribe({
      next: (data) => {
        this.keyMetrics.set(data.keyMetrics);
        this.recruitmentFunnel.set(data.recruitmentFunnel);
        this.offersOverview.set(data.offersOverview);
        this.staffPerformance.set(data.staffPerformance);
        this.pendingApprovals.set(data.pendingApprovals);
        this.recentActivity.set(data.recentActivity);
        
        this.isRefreshing.set(false);
        this.updateCharts();
      },
      error: () => this.isRefreshing.set(false)
    });
  }

  // ============================================
  // DATE RANGE
  // ============================================

  selectDateRange(range: DateRange): void {
    this.selectedDateRange.set(range);
    this.filterForm.patchValue({
      startDate: range.start,
      endDate: range.end
    });
    this.showDatePicker.set(false);
    this.loadDashboard();
  }

  applyCustomDateRange(): void {
    const start = this.filterForm.value.startDate;
    const end = this.filterForm.value.endDate;
    
    this.selectedDateRange.set({
      start,
      end,
      label: 'Custom Range'
    });
    
    this.showDatePicker.set(false);
    this.loadDashboard();
  }

  // ============================================
  // CHARTS
  // ============================================

  private renderCharts(): void {
    this.renderFunnelChart();
    this.renderOffersTimeChart();
    this.renderOffersBreakdownChart();
  }

  private renderFunnelChart(): void {
    if (!this.funnelChartRef) return;
    
    const funnel = this.recruitmentFunnel();
    const ctx = this.funnelChartRef.nativeElement.getContext('2d');
    if (!ctx) return;

    // Destroy existing chart
    if (this.funnelChart) {
      this.funnelChart.destroy();
    }

    const config: ChartConfiguration = {
      type: 'bar',
      data: {
        labels: funnel.map(s => s.stage),
        datasets: [{
          label: 'Students',
          data: funnel.map(s => s.count),
          backgroundColor: funnel.map(s => FUNNEL_STAGE_COLORS[s.stage]),
          borderRadius: 8
        }]
      },
      options: {
        indexAxis: 'y',
        responsive: true,
        maintainAspectRatio: false,
        onClick: (event, elements) => {
          if (elements.length > 0) {
            const index = elements[0].index;
            const stage = funnel[index].stage;
            this.onFunnelStageClick(stage);
          }
        },
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              afterLabel: (context) => {
                const stage = funnel[context.dataIndex];
                return `${stage.percentage.toFixed(1)}% of total`;
              }
            }
          }
        },
        scales: {
          x: {
            beginAtZero: true,
            grid: { display: false }
          },
          y: {
            grid: { display: false }
          }
        }
      }
    };

    this.funnelChart = new Chart(ctx, config);
  }

  private renderOffersTimeChart(): void {
    if (!this.offersTimeChartRef) return;
    
    const overview = this.offersOverview();
    if (!overview) return;
    
    const ctx = this.offersTimeChartRef.nativeElement.getContext('2d');
    if (!ctx) return;

    if (this.offersTimeChart) {
      this.offersTimeChart.destroy();
    }

    const config: ChartConfiguration = {
      type: 'line',
      data: {
        labels: overview.offersByMonth.map((d: any) => d.month),
        datasets: [
          {
            label: 'Total Offers',
            data: overview.offersByMonth.map((d: any) => d.totalOffers),
            borderColor: '#3B82F6',
            backgroundColor: 'rgba(59, 130, 246, 0.1)',
            fill: true,
            tension: 0.4
          },
          {
            label: 'Accepted',
            data: overview.offersByMonth.map((d: any) => d.acceptedOffers),
            borderColor: '#10B981',
            backgroundColor: 'rgba(16, 185, 129, 0.1)',
            fill: true,
            tension: 0.4
          },
          {
            label: 'Declined',
            data: overview.offersByMonth.map((d: any) => d.declinedOffers),
            borderColor: '#EF4444',
            backgroundColor: 'rgba(239, 68, 68, 0.1)',
            fill: true,
            tension: 0.4
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { position: 'top' }
        },
        scales: {
          y: { beginAtZero: true }
        }
      }
    };

    this.offersTimeChart = new Chart(ctx, config);
  }

  private renderOffersBreakdownChart(): void {
    if (!this.offersBreakdownChartRef) return;
    
    const overview = this.offersOverview();
    if (!overview) return;
    
    const ctx = this.offersBreakdownChartRef.nativeElement.getContext('2d');
    if (!ctx) return;

    if (this.offersBreakdownChart) {
      this.offersBreakdownChart.destroy();
    }

    const config: ChartConfiguration = {
      type: 'doughnut',
      data: {
        labels: overview.offersByInstrument.map((d: any) => d.label),
        datasets: [{
          data: overview.offersByInstrument.map((d: any) => d.count),
          backgroundColor: [
            '#3B82F6', '#8B5CF6', '#EC4899', '#F59E0B', 
            '#10B981', '#6366F1', '#14B8A6', '#F97316'
          ]
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { position: 'right' }
        }
      }
    };

    this.offersBreakdownChart = new Chart(ctx, config);
  }

  private updateCharts(): void {
    // Update chart data without recreating
    if (this.funnelChart) {
      const funnel = this.recruitmentFunnel();
      this.funnelChart.data.datasets[0].data = funnel.map(s => s.count);
      this.funnelChart.update();
    }
    
    // Similar for other charts...
  }

  private destroyCharts(): void {
    this.funnelChart?.destroy();
    this.offersTimeChart?.destroy();
    this.offersBreakdownChart?.destroy();
  }

  // ============================================
  // FUNNEL INTERACTION
  // ============================================

  onFunnelStageClick(stage: string): void {
    this.selectedFunnelStage.set(stage);
    
    const filters: DirectorDashboardFilters = {
      dateRangeStart: this.filterForm.value.startDate,
      dateRangeEnd: this.filterForm.value.endDate
    };
    
    this.dashboardService.getFunnelStageStudents(stage, filters).subscribe({
      next: (students) => {
        // Would open a modal or side panel with student list
        console.log(`${stage} students:`, students);
      }
    });
  }

  // ============================================
  // STAFF MANAGEMENT
  // ============================================

  sortStaff(column: string): void {
    const current = this.staffSortColumn();
    
    if (current === column) {
      this.staffSortDirection.set(
        this.staffSortDirection() === 'asc' ? 'desc' : 'asc'
      );
    } else {
      this.staffSortColumn.set(column);
      this.staffSortDirection.set('desc');
    }
  }

  viewStaffProfile(staffId: number): void {
    // Navigate to staff profile or open modal
    console.log('View staff:', staffId);
  }

  adjustBudget(staff: StaffPerformanceDto): void {
    const newBudget = prompt(`Enter new budget for ${staff.staffName}:`, 
      staff.totalBudgetAllocated.toString());
    
    if (newBudget) {
      this.dashboardService.updateStaffBudget(staff.staffId, parseFloat(newBudget))
        .subscribe(() => this.refreshDashboard());
    }
  }

  // ============================================
  // APPROVALS
  // ============================================

  approveOffer(approval: PendingApprovalDto): void {
    if (!confirm(`Approve ${approval.studentName}'s offer of $${approval.amount}?`)) {
      return;
    }

    this.dashboardService.approveOffer(approval.approvalId).subscribe({
      next: () => {
        this.pendingApprovals.update(approvals => 
          approvals.filter(a => a.approvalId !== approval.approvalId)
        );
      }
    });
  }

  denyOffer(approval: PendingApprovalDto): void {
    const reason = prompt('Reason for denial:');
    if (!reason) return;

    this.dashboardService.denyOffer(approval.approvalId, reason).subscribe({
      next: () => {
        this.pendingApprovals.update(approvals => 
          approvals.filter(a => a.approvalId !== approval.approvalId)
        );
      }
    });
  }

  // ============================================
  // REAL-TIME UPDATES
  // ============================================

  private setupRealTimeUpdates(): void {
    this.dashboardService.startSignalR();
    
    this.dashboardService.updates$
      .pipe(takeUntil(this.destroy$))
      .subscribe(update => {
        if (!update) return;
        
        switch (update.type) {
          case 'NewActivity':
            this.recentActivity.update(activities => 
              [update.data, ...activities].slice(0, 20)
            );
            break;
            
          case 'MetricUpdate':
            this.keyMetrics.set(update.data);
            break;
            
          case 'ApprovalNeeded':
            this.pendingApprovals.update(approvals => 
              [update.data, ...approvals]
            );
            break;
        }
      });
  }

  private setupAutoRefresh(): void {
    interval(this.refreshInterval)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.refreshDashboard();
      });
  }

  // ============================================
  // EXPORT
  // ============================================

  exportDashboard(format: 'csv' | 'excel' | 'pdf'): void {
    const options: ExportOptions = {
      format,
      includeCharts: format === 'pdf',
      dateRange: this.selectedDateRange(),
      sections: ['metrics', 'funnel', 'offers', 'staff', 'approvals']
    };

    this.dashboardService.exportDashboard(options).subscribe({
      next: (blob) => {
        const filename = `dashboard-${new Date().toISOString().split('T')[0]}.${format}`;
        this.dashboardService.downloadExport(blob, filename);
        this.showExportModal.set(false);
      }
    });
  }

  // ============================================
  // UTILITIES
  // ============================================

  formatCurrency(amount: number): string {
    return this.dashboardService.formatCurrency(amount);
  }

  formatPercentage(value: number): string {
    return this.dashboardService.formatPercentage(value);
  }

  getActivityIcon(type: string): string {
    return ACTIVITY_CONFIG[type]?.icon || '';
  }

  getActivityColor(type: string): string {
    return ACTIVITY_CONFIG[type]?.color || 'gray';
  }

  getChangeClass(change: number): string {
    if (change > 0) return 'text-green-600';
    if (change < 0) return 'text-red-600';
    return 'text-gray-600';
  }

  getChangeIcon(change: number): string {
    if (change > 0) return 'M5 10l7-7m0 0l7 7m-7-7v18';
    if (change < 0) return 'M19 14l-7 7m0 0l-7-7m7 7V3';
    return 'M5 12h14';
  }
}