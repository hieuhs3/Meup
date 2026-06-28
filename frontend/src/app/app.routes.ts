import { Routes } from '@angular/router';
import { adminGuard, authGuard } from './core/guards/auth.guards';
import { Login } from './features/auth/login';
import { Register } from './features/auth/register';
import { ForgotPassword } from './features/auth/forgot-password';
import { ResetPassword } from './features/auth/reset-password';
import { ConfirmEmail } from './features/auth/confirm-email';
import { Notifications } from './features/notifications/notifications';
import { Insights } from './features/insights/insights';
import { Shell } from './layout/shell';
import { Today } from './features/today/today';
import { Profile } from './features/profile/profile';
import { Finance } from './features/finance/finance';
import { Health } from './features/health/health';
import { Work } from './features/work/work';
import { Journal } from './features/journal/journal';
import { Career } from './features/career/career';
import { Documents } from './features/documents/documents';
import { Knowledge } from './features/knowledge/knowledge';
import { Calendar } from './features/calendar/calendar';
import { StatsPage } from './features/stats/stats';
import { Search } from './features/search/search';
import { Settings } from './features/settings/settings';
import { AdminUsers } from './features/admin/admin-users';

export const routes: Routes = [
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: 'forgot-password', component: ForgotPassword },
  { path: 'reset-password', component: ResetPassword },
  { path: 'confirm-email', component: ConfirmEmail },
  {
    path: 'app',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: 'today', component: Today },
      { path: 'finance', component: Finance },
      { path: 'health', component: Health },
      { path: 'work', component: Work },
      { path: 'calendar', component: Calendar },
      { path: 'journal', component: Journal },
      { path: 'knowledge', component: Knowledge },
      { path: 'career', component: Career },
      { path: 'documents', component: Documents },
      { path: 'stats', component: StatsPage },
      { path: 'insights', component: Insights },
      { path: 'search', component: Search },
      { path: 'notifications', component: Notifications },
      { path: 'settings', component: Settings },
      { path: 'profile', component: Profile },
      { path: 'admin', component: AdminUsers, canActivate: [adminGuard] },
      { path: '', pathMatch: 'full', redirectTo: 'today' },
    ],
  },
  { path: '', pathMatch: 'full', redirectTo: 'app' },
  { path: '**', redirectTo: 'app' },
];
