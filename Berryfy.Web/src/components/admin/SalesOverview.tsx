import { Order, OrderStatus } from '../../types/order';

export default function SalesOverview({ orders }: { orders: Order[] }) {
  const now = new Date();
  const months = Array.from({ length: 6 }, (_, index) => {
    const date = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() - 5 + index, 1));
    const key = date.toISOString().slice(0, 7);
    const total = orders.filter(order => order.isPaid &&
      order.status !== OrderStatus.Refunded && order.status !== OrderStatus.Cancelled &&
      order.createdAt.slice(0, 7) === key).reduce((sum, order) => sum + order.total, 0);
    return { key, label: date.toLocaleDateString('en-US', { month: 'short', year: 'numeric', timeZone: 'UTC' }), total };
  });
  const maximum = Math.max(...months.map(month => month.total), 1);

  return (
    <div>
      <p className="small text-muted">Paid order totals by creation month (UTC), from the latest {orders.length} orders. Fully refunded and cancelled orders are excluded; partial refunds are not deducted.</p>
      {months.every(month => month.total === 0) && <p>No paid orders in this period.</p>}
      <table className="table table-sm align-middle">
        <caption className="visually-hidden">Paid order totals for the last six months in US dollars</caption>
        <thead><tr><th scope="col">Month</th><th scope="col">Sales</th><th scope="col" className="text-end">USD</th></tr></thead>
        <tbody>{months.map(month => (
          <tr key={month.key}>
            <th scope="row" className="fw-normal text-nowrap">{month.label}</th>
            <td className="w-50"><div className="progress" style={{ height: 12 }} aria-hidden="true">
              <div className="progress-bar bg-primary" style={{ width: `${month.total / maximum * 100}%` }} />
            </div></td>
            <td className="text-end">${month.total.toFixed(2)}</td>
          </tr>
        ))}</tbody>
      </table>
    </div>
  );
}
